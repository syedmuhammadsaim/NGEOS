using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NGEOS.Data;
using NGEOS.Models;
using NGEOS.Services.Payments.Configuration;
using NGEOS.Services.Payments.Core;
using NGEOS.Services.Payments.Dtos;
using NGEOS.ViewModels;

namespace NGEOS.Services.Payments.Orchestration
{
    public class PaymentOrchestrationService : IPaymentOrchestrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentGatewayFactory _gatewayFactory;
        private readonly PaymentOptions _options;
        private readonly ILogger<PaymentOrchestrationService> _logger;

        public PaymentOrchestrationService(
            ApplicationDbContext context,
            IPaymentGatewayFactory gatewayFactory,
            IOptions<PaymentOptions> options,
            ILogger<PaymentOrchestrationService> logger)
        {
            _context = context;
            _gatewayFactory = gatewayFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<PaymentInitiateResult> StartDonationPaymentAsync(
            DonateRequestViewModel model,
            int? userId,
            string clientIp,
            string baseUrl,
            CancellationToken cancellationToken = default)
        {
            var selectedGateway = string.IsNullOrWhiteSpace(model.PaymentGateway) ? "Safepay" : model.PaymentGateway.Trim();

            // 1. Fetch selected Cause
            var cause = await _context.Causes.Include(c => c.Ngo).FirstOrDefaultAsync(c => c.Id == model.CauseId && c.IsActive, cancellationToken);
            if (cause == null)
            {
                return new PaymentInitiateResult
                {
                    IsSuccess = false,
                    ErrorMessage = "The selected welfare cause does not exist or is inactive."
                };
            }

            // 2. Generate unique internal transaction reference
            var randomHex = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            var txnId = $"GA-TXN-{DateTime.UtcNow:yyyyMM}-{randomHex}";

            var donationType = (model.DonationType?.Equals("Zakat", StringComparison.OrdinalIgnoreCase) == true) ? "Zakat" : "General";
            var userRawNote = model.Notes?.Trim() ?? "";
            var initialNotes = string.IsNullOrWhiteSpace(userRawNote)
                ? $"[Type:{donationType}] [Email:Pending] [Gateway:{selectedGateway}]"
                : $"[Type:{donationType}] [Email:Pending] [Gateway:{selectedGateway}] {userRawNote}";

            // 3. Create initial Donation record with status 'Pending'
            var donation = new Donation
            {
                TransactionId = txnId,
                CauseId = cause.Id,
                ProgrammeId = model.ProgrammeId,
                UserId = userId,
                DonorName = model.DonorName.Trim(),
                DonorEmail = model.DonorEmail.Trim(),
                DonorPhone = model.DonorPhone?.Trim(),
                Amount = model.Amount,
                Currency = string.IsNullOrWhiteSpace(model.Currency) ? "PKR" : model.Currency.ToUpperInvariant(),
                PaymentGateway = selectedGateway,
                CardType = selectedGateway,
                CardHolderName = model.DonorName.Trim(),
                MaskedCardNumber = "N/A (Hosted Gateway)",
                PaymentStatus = "Pending",
                Notes = initialNotes,
                DonationDate = DateTime.UtcNow
            };

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Create technical audit transaction record
            var txnRecord = new PaymentTransaction
            {
                DonationId = donation.Id,
                GatewayName = selectedGateway,
                InternalReference = txnId,
                Amount = model.Amount,
                Currency = donation.Currency,
                Status = "Initiated",
                Environment = _options.GlobalEnvironment,
                VerificationStatus = "Pending",
                IdempotencyKey = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactions.Add(txnRecord);
            await _context.SaveChangesAsync(cancellationToken);

            // 5. Select gateway from factory and call InitiatePaymentAsync
            try
            {
                var gateway = _gatewayFactory.GetGateway(selectedGateway);

                var returnUrl = $"{baseUrl.TrimEnd('/')}/payments/callback/{selectedGateway.ToLowerInvariant()}?ref={txnId}";
                var cancelUrl = $"{baseUrl.TrimEnd('/')}/Donate?causeId={cause.Id}&cancelled=true";
                var webhookUrl = $"{baseUrl.TrimEnd('/')}/api/payments/webhooks/{selectedGateway.ToLowerInvariant()}";

                var initRequest = new PaymentInitiateRequest
                {
                    DonationId = donation.Id,
                    InternalReference = txnId,
                    Amount = donation.Amount,
                    Currency = donation.Currency,
                    DonationType = donationType,
                    ItemTitle = $"Donation: {cause.Title}",
                    ItemDescription = cause.ShortDescription,
                    DonorName = donation.DonorName,
                    DonorEmail = donation.DonorEmail,
                    DonorPhone = donation.DonorPhone,
                    ReturnUrl = returnUrl,
                    CancelUrl = cancelUrl,
                    WebhookUrl = webhookUrl,
                    ClientIpAddress = clientIp
                };

                var initResult = await gateway.InitiatePaymentAsync(initRequest, cancellationToken);
                initResult.InternalReference = txnId;
                initResult.Amount = donation.Amount;
                initResult.Currency = donation.Currency;

                // Update transaction with gateway reference and payload
                txnRecord.GatewayReference = initResult.GatewayReference;
                txnRecord.Status = initResult.IsSuccess ? "Pending" : "Failed";
                txnRecord.ErrorCode = initResult.ErrorCode;
                txnRecord.ErrorMessage = initResult.ErrorMessage;
                txnRecord.RawCallbackPayload = initResult.RawPayload;

                await _context.SaveChangesAsync(cancellationToken);

                return initResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate payment on {Gateway} for {Ref}", selectedGateway, txnId);
                txnRecord.Status = "Failed";
                txnRecord.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync(cancellationToken);

                return new PaymentInitiateResult
                {
                    IsSuccess = false,
                    GatewayName = selectedGateway,
                    ErrorMessage = $"Failed to connect to {selectedGateway}: {ex.Message}"
                };
            }
        }
    }
}
