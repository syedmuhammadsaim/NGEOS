using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NGEOS.Data;
using NGEOS.Models;
using NGEOS.Services;
using NGEOS.Services.Payments.Core;
using NGEOS.Services.Payments.Dtos;

namespace NGEOS.Services.Payments.Orchestration
{
    public class PaymentVerificationService : IPaymentVerificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentGatewayFactory _gatewayFactory;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PaymentVerificationService> _logger;

        public PaymentVerificationService(
            ApplicationDbContext context,
            IPaymentGatewayFactory gatewayFactory,
            IEmailSender emailSender,
            ILogger<PaymentVerificationService> logger)
        {
            _context = context;
            _gatewayFactory = gatewayFactory;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<PaymentVerifyResult> ProcessCallbackVerificationAsync(
            string gatewayName,
            PaymentCallbackData callbackData,
            CancellationToken cancellationToken = default)
        {
            var internalRef = callbackData.InternalReference;
            var gatewayRef = callbackData.GatewayReference;

            // 1. Locate PaymentTransaction record
            var txn = await _context.PaymentTransactions
                .Include(pt => pt.Donation)
                .ThenInclude(d => d!.Cause)
                .ThenInclude(c => c!.Ngo)
                .FirstOrDefaultAsync(pt =>
                    (!string.IsNullOrEmpty(internalRef) && pt.InternalReference == internalRef) ||
                    (!string.IsNullOrEmpty(gatewayRef) && pt.GatewayReference == gatewayRef),
                    cancellationToken);

            if (txn == null || txn.Donation == null)
            {
                return new PaymentVerifyResult
                {
                    IsVerified = false,
                    GatewayName = gatewayName,
                    Status = "Failed",
                    ErrorMessage = "No matching pending donation found for this payment reference."
                };
            }

            // 2. IDEMPOTENCY GUARD: If already verified & completed, return immediately without duplicate processing
            if (txn.Status == "Completed" && txn.Donation.PaymentStatus == "Completed")
            {
                _logger.LogInformation("Idempotent callback received: Txn {Ref} already completed.", txn.InternalReference);
                return new PaymentVerifyResult
                {
                    IsVerified = true,
                    IsPaymentSuccessful = true,
                    Status = "Completed",
                    GatewayName = gatewayName,
                    InternalReference = txn.InternalReference,
                    GatewayReference = txn.GatewayReference,
                    VerifiedAmount = txn.Amount,
                    VerifiedCurrency = txn.Currency
                };
            }

            // 3. Delegate cryptographic and server inquiry verification to gateway service
            var gateway = _gatewayFactory.GetGateway(gatewayName);
            var verifyResult = await gateway.VerifyPaymentAsync(callbackData, cancellationToken);

            txn.RawCallbackPayload = callbackData.RawBody;
            txn.ErrorCode = verifyResult.ErrorCode;
            txn.ErrorMessage = verifyResult.ErrorMessage;

            // 4. Validate Amount and Currency strictly (defence against payload tampering)
            if (verifyResult.IsPaymentSuccessful)
            {
                if (verifyResult.VerifiedAmount.HasValue && Math.Abs(verifyResult.VerifiedAmount.Value - txn.Amount) > 0.05m)
                {
                    _logger.LogError("CRITICAL AMOUNT MISMATCH: Expected {Expected}, Gateway verified {Actual} for Txn {Ref}",
                        txn.Amount, verifyResult.VerifiedAmount.Value, txn.InternalReference);

                    txn.Status = "Failed";
                    txn.VerificationStatus = "Mismatch";
                    txn.ErrorMessage = "Paid amount does not match original donation amount.";
                    txn.Donation.PaymentStatus = "Failed";
                    await _context.SaveChangesAsync(cancellationToken);

                    verifyResult.IsPaymentSuccessful = false;
                    verifyResult.Status = "Failed";
                    verifyResult.ErrorMessage = txn.ErrorMessage;
                    return verifyResult;
                }

                // Mark Completed
                txn.Status = "Completed";
                txn.VerificationStatus = "Verified";
                txn.CompletedAt = DateTime.UtcNow;
                txn.Donation.PaymentStatus = "Completed";

                // Increment Cause raised amount
                if (txn.Donation.Cause != null)
                {
                    txn.Donation.Cause.RaisedAmount += txn.Donation.Amount;
                }

                await _context.SaveChangesAsync(cancellationToken);

                // Send formal confirmation receipt email
                try
                {
                    await _emailSender.SendDonationReceiptAsync(
                        txn.Donation.DonorEmail,
                        txn.Donation.DonorName,
                        txn.Donation.TransactionId,
                        txn.Donation.Amount,
                        txn.Donation.DonationType,
                        txn.Donation.Cause?.Title ?? "General Charity Relief",
                        txn.Donation.Cause?.Ngo?.Name ?? "NGEOS Foundation",
                        txn.Donation.DonationDate,
                        txn.Donation.PaymentGateway,
                        txn.Donation.MaskedCardNumber
                    );

                    txn.Donation.Notes = (txn.Donation.Notes ?? "").Replace("[Email:Pending]", "[Email:Sent]");
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to email receipt for {TxnId}", txn.Donation.TransactionId);
                }
            }
            else
            {
                txn.Status = verifyResult.Status;
                txn.VerificationStatus = "Failed";
                txn.Donation.PaymentStatus = verifyResult.Status;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return verifyResult;
        }

        public async Task<PaymentRefundResult> RequestRefundAsync(
            int donationId,
            decimal refundAmount,
            string reason,
            string adminEmail,
            CancellationToken cancellationToken = default)
        {
            var donation = await _context.Donations
                .Include(d => d.Transactions)
                .Include(d => d.Cause)
                .FirstOrDefaultAsync(d => d.Id == donationId, cancellationToken);

            if (donation == null)
            {
                return new PaymentRefundResult { IsSuccess = false, ErrorMessage = "Donation record not found." };
            }

            if (donation.PaymentStatus != "Completed")
            {
                return new PaymentRefundResult { IsSuccess = false, ErrorMessage = "Only completed donations can be refunded." };
            }

            if (refundAmount <= 0 || refundAmount > donation.Amount)
            {
                return new PaymentRefundResult { IsSuccess = false, ErrorMessage = "Invalid refund amount requested." };
            }

            var txn = donation.Transactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
            var gatewayName = donation.PaymentGateway;

            try
            {
                var gateway = _gatewayFactory.GetGateway(gatewayName);
                var refundReq = new PaymentRefundRequest
                {
                    DonationId = donation.Id,
                    InternalReference = donation.TransactionId,
                    GatewayReference = txn?.GatewayReference ?? donation.TransactionId,
                    RefundAmount = refundAmount,
                    Currency = donation.Currency,
                    Reason = reason,
                    AdminEmail = adminEmail
                };

                var result = await gateway.ProcessRefundAsync(refundReq, cancellationToken);

                if (result.IsSuccess)
                {
                    donation.PaymentStatus = "Refunded";
                    if (donation.Cause != null)
                    {
                        donation.Cause.RaisedAmount = Math.Max(0, donation.Cause.RaisedAmount - refundAmount);
                    }

                    if (txn != null)
                    {
                        txn.Status = "Refunded";
                        txn.RefundAmount = refundAmount;
                        txn.RefundReference = result.RefundReference;
                        txn.RefundReason = reason;
                        txn.RefundedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception initiating refund for donation {Id}", donationId);
                return new PaymentRefundResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }
    }
}
