using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NGEOS.Data;
using NGEOS.Models;
using NGEOS.Services;
using NGEOS.Services.Payments.Core;
using NGEOS.Services.Payments.Dtos;
using NGEOS.Services.Payments.Orchestration;
using NGEOS.ViewModels;

namespace NGEOS.Controllers
{
    public class DonateController : Controller
    {
        private const string PendingDonationSessionKey = "PendingDonation_Data";
        private readonly ApplicationDbContext _context;
        private readonly IPaymentOrchestrationService _orchestrationService;
        private readonly IPaymentGatewayFactory _gatewayFactory;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<DonateController> _logger;

        public DonateController(
            ApplicationDbContext context,
            IPaymentOrchestrationService orchestrationService,
            IPaymentGatewayFactory gatewayFactory,
            IEmailSender emailSender,
            ILogger<DonateController> logger)
        {
            _context = context;
            _orchestrationService = orchestrationService;
            _gatewayFactory = gatewayFactory;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? causeId, int? programmeId, decimal? amount = null, string? donationType = null)
        {
            var causes = await _context.Causes
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.IsFeatured)
                .ToListAsync();

            var selectedType = !string.IsNullOrWhiteSpace(donationType) && donationType.Equals("Zakat", StringComparison.OrdinalIgnoreCase)
                ? "Zakat"
                : "General";

            var model = new DonateRequestViewModel
            {
                CauseId = causeId ?? (causes.FirstOrDefault()?.Id ?? 0),
                ProgrammeId = programmeId,
                Amount = amount.HasValue && amount.Value > 0 ? Math.Round(amount.Value, 2) : 1000,
                DonationType = selectedType,
                PaymentGateway = "Safepay",
                Currency = "PKR",
                AvailableCauses = causes
            };

            // Pre-fill user data if authenticated
            if (User.Identity?.IsAuthenticated == true)
            {
                model.DonorName = User.Identity.Name ?? "";
                model.DonorEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "";
                model.CardHolderName = model.DonorName;
            }

            // Expose supported gateways capability info to the view
            ViewBag.AvailableGateways = _gatewayFactory.GetAllGateways().Select(g => g.Capabilities).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDonation(DonateRequestViewModel model)
        {
            // If user is not logged in, immediately store filled data in session and redirect to Login
            if (User.Identity?.IsAuthenticated != true)
            {
                model.AvailableCauses = null;
                var serialized = JsonSerializer.Serialize(model);
                HttpContext.Session.SetString(PendingDonationSessionKey, serialized);

                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("FinalizePendingDonation", "Donate") });
            }

            var causes = await _context.Causes.Where(c => c.IsActive).ToListAsync();
            model.AvailableCauses = causes;
            ViewBag.AvailableGateways = _gatewayFactory.GetAllGateways().Select(g => g.Capabilities).ToList();

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var cause = await _context.Causes.Include(c => c.Ngo).FirstOrDefaultAsync(c => c.Id == model.CauseId && c.IsActive);
            if (cause == null)
            {
                ModelState.AddModelError("CauseId", "Selected cause does not exist or is inactive.");
                return View("Index", model);
            }

            return await InitiateGatewayDonationAsync(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> FinalizePendingDonation()
        {
            var serialized = HttpContext.Session.GetString(PendingDonationSessionKey);
            if (string.IsNullOrEmpty(serialized))
            {
                return RedirectToAction("Index");
            }

            DonateRequestViewModel? model = null;
            try
            {
                model = JsonSerializer.Deserialize<DonateRequestViewModel>(serialized);
            }
            catch
            {
                HttpContext.Session.Remove(PendingDonationSessionKey);
                return RedirectToAction("Index");
            }

            if (model == null)
            {
                HttpContext.Session.Remove(PendingDonationSessionKey);
                return RedirectToAction("Index");
            }

            // Remove from session to prevent duplicate runs
            HttpContext.Session.Remove(PendingDonationSessionKey);

            // If donor details were blank, fallback to authenticated user claims
            if (string.IsNullOrWhiteSpace(model.DonorName) && !string.IsNullOrWhiteSpace(User.Identity?.Name))
            {
                model.DonorName = User.Identity.Name;
            }
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(model.DonorEmail) && !string.IsNullOrWhiteSpace(emailClaim))
            {
                model.DonorEmail = emailClaim;
            }

            return await InitiateGatewayDonationAsync(model);
        }

        private async Task<IActionResult> InitiateGatewayDonationAsync(DonateRequestViewModel model)
        {
            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int parsedId))
                {
                    userId = parsedId;
                }
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

            var initResult = await _orchestrationService.StartDonationPaymentAsync(model, userId, clientIp, baseUrl);

            if (!initResult.IsSuccess)
            {
                ModelState.AddModelError("", initResult.ErrorMessage ?? "Payment gateway initialization failed. Please choose another payment method or try again.");
                model.AvailableCauses = await _context.Causes.Where(c => c.IsActive).ToListAsync();
                ViewBag.AvailableGateways = _gatewayFactory.GetAllGateways().Select(g => g.Capabilities).ToList();
                return View("Index", model);
            }

            switch (initResult.ActionType)
            {
                case PaymentActionType.Redirect:
                    return Redirect(initResult.RedirectUrl);

                case PaymentActionType.PostForm:
                    return View("PaymentRedirect", initResult);

                case PaymentActionType.DirectSuccess:
                    TempData["SuccessMessage"] = $"Thank you for your generous contribution of {initResult.Amount:N2} {initResult.Currency}!";
                    return RedirectToAction("Confirmation", new { transactionId = initResult.InternalReference });

                default:
                    return RedirectToAction("PaymentResult", new
                    {
                        status = "Failed",
                        reference = initResult.InternalReference,
                        gateway = initResult.GatewayName,
                        message = "Unexpected payment response from the gateway."
                    });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return RedirectToAction("Index");
            }

            var donation = await _context.Donations
                .Include(d => d.Cause)
                .ThenInclude(c => c!.Ngo)
                .Include(d => d.Transactions)
                .FirstOrDefaultAsync(d => d.TransactionId == transactionId);

            if (donation == null)
            {
                return NotFound();
            }

            var latestTxn = donation.Transactions?.OrderByDescending(t => t.Id).FirstOrDefault();

            var receipt = new DonationReceiptViewModel
            {
                TransactionId = donation.TransactionId,
                GatewayReference = latestTxn?.GatewayReference ?? donation.TransactionId,
                PaymentGateway = donation.PaymentGateway ?? donation.CardType ?? "Online",
                Currency = donation.Currency ?? "PKR",
                CauseTitle = donation.Cause?.Title ?? "General Charity Relief",
                Category = donation.Cause?.Category ?? "Welfare",
                NgoName = donation.Cause?.Ngo?.Name ?? "NGEOS Foundation",
                DonorName = donation.DonorName,
                DonorEmail = donation.DonorEmail,
                DonorPhone = donation.DonorPhone,
                Amount = donation.Amount,
                CardType = donation.PaymentGateway ?? donation.CardType ?? "Online",
                MaskedCardNumber = donation.MaskedCardNumber,
                DonationDate = donation.DonationDate,
                PaymentStatus = donation.PaymentStatus,
                DonationType = donation.DonationType,
                IsReceiptEmailSent = donation.IsReceiptEmailSent,
                Notes = donation.CleanNotes
            };

            return View(receipt);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentResult(string? status, string? reference, string? gateway, string? message)
        {
            ViewBag.Status = status ?? "Failed";
            ViewBag.Reference = reference ?? "N/A";
            ViewBag.Gateway = gateway ?? "Payment Gateway";
            ViewBag.Message = message ?? "Your transaction could not be verified or was cancelled by the user.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadReceipt(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return RedirectToAction("Index");
            }

            var donation = await _context.Donations
                .Include(d => d.Cause)
                .ThenInclude(c => c!.Ngo)
                .Include(d => d.Transactions)
                .FirstOrDefaultAsync(d => d.TransactionId == transactionId);

            if (donation == null)
            {
                return NotFound();
            }

            var latestTxn = donation.Transactions?.OrderByDescending(t => t.Id).FirstOrDefault();

            var receipt = new DonationReceiptViewModel
            {
                TransactionId = donation.TransactionId,
                GatewayReference = latestTxn?.GatewayReference ?? donation.TransactionId,
                PaymentGateway = donation.PaymentGateway ?? donation.CardType ?? "Online",
                Currency = donation.Currency ?? "PKR",
                CauseTitle = donation.Cause?.Title ?? "General Charity Relief",
                Category = donation.Cause?.Category ?? "Welfare",
                NgoName = donation.Cause?.Ngo?.Name ?? "NGEOS Foundation",
                DonorName = donation.DonorName,
                DonorEmail = donation.DonorEmail,
                DonorPhone = donation.DonorPhone,
                Amount = donation.Amount,
                CardType = donation.PaymentGateway ?? donation.CardType ?? "Online",
                MaskedCardNumber = donation.MaskedCardNumber,
                DonationDate = donation.DonationDate,
                PaymentStatus = donation.PaymentStatus,
                DonationType = donation.DonationType,
                IsReceiptEmailSent = donation.IsReceiptEmailSent,
                Notes = donation.CleanNotes
            };

            ViewBag.PrintMode = true;
            return View("ReceiptPrint", receipt);
        }
    }
}
