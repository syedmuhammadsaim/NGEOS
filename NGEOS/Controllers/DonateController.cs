using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Models;
using NGEOS.Services;
using NGEOS.ViewModels;

namespace NGEOS.Controllers
{
    public class DonateController : Controller
    {
        private const string PendingDonationSessionKey = "PendingDonation_Data";
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<DonateController> _logger;

        public DonateController(ApplicationDbContext context, IEmailSender emailSender, ILogger<DonateController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? causeId, decimal? amount = null, string? donationType = null)
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
                Amount = amount.HasValue && amount.Value > 0 ? Math.Round(amount.Value, 2) : 50,
                DonationType = selectedType,
                AvailableCauses = causes
            };

            // Pre-fill user data if authenticated
            if (User.Identity?.IsAuthenticated == true)
            {
                model.DonorName = User.Identity.Name ?? "";
                model.DonorEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "";
                model.CardHolderName = model.DonorName;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDonation(DonateRequestViewModel model)
        {
            // If user is not logged in, immediately store filled data in session and redirect to Login!
            if (User.Identity?.IsAuthenticated != true)
            {
                model.AvailableCauses = null;
                var serialized = JsonSerializer.Serialize(model);
                HttpContext.Session.SetString(PendingDonationSessionKey, serialized);

                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("FinalizePendingDonation", "Donate") });
            }

            var causes = await _context.Causes.Where(c => c.IsActive).ToListAsync();
            model.AvailableCauses = causes;

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

            return await ExecuteDonationAsync(model, cause);
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

            // Remove from session to prevent duplicate charges
            HttpContext.Session.Remove(PendingDonationSessionKey);

            var cause = await _context.Causes.Include(c => c.Ngo).FirstOrDefaultAsync(c => c.Id == model.CauseId && c.IsActive);
            if (cause == null)
            {
                cause = await _context.Causes.Include(c => c.Ngo).FirstOrDefaultAsync(c => c.IsActive);
            }

            if (cause == null)
            {
                TempData["ErrorMessage"] = "No active campaign found.";
                return RedirectToAction("Index");
            }

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

            if (string.IsNullOrWhiteSpace(model.CardNumber)) model.CardNumber = "4111 2222 3333 4444";
            if (string.IsNullOrWhiteSpace(model.CardHolderName)) model.CardHolderName = model.DonorName ?? "Valued Donor";
            if (string.IsNullOrWhiteSpace(model.CardType)) model.CardType = "Credit Card";

            return await ExecuteDonationAsync(model, cause);
        }

        private async Task<IActionResult> ExecuteDonationAsync(DonateRequestViewModel model, Cause cause)
        {
            // Clean card number and create masked version (e.g., "**** **** **** 1234")
            var digitsOnly = new string((model.CardNumber ?? "").Where(char.IsDigit).ToArray());
            var last4 = digitsOnly.Length >= 4 ? digitsOnly.Substring(digitsOnly.Length - 4) : "4242";
            var maskedCard = $"****-****-****-{last4}";

            // Generate unique realistic transaction ID
            var randomHex = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            var txnId = $"GA-TXN-{DateTime.UtcNow:yyyyMM}-{randomHex}";

            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int parsedId))
                {
                    userId = parsedId;
                }
            }

            var donationType = (model.DonationType?.Equals("Zakat", StringComparison.OrdinalIgnoreCase) == true) ? "Zakat" : "General";
            var userRawNote = model.Notes?.Trim() ?? "";
            var initialNotes = string.IsNullOrWhiteSpace(userRawNote)
                ? $"[Type:{donationType}] [Email:Pending]"
                : $"[Type:{donationType}] [Email:Pending] {userRawNote}";

            var donation = new Donation
            {
                TransactionId = txnId,
                CauseId = cause.Id,
                UserId = userId,
                DonorName = model.DonorName.Trim(),
                DonorEmail = model.DonorEmail.Trim(),
                DonorPhone = model.DonorPhone?.Trim(),
                Amount = model.Amount,
                CardType = model.CardType,
                CardHolderName = model.CardHolderName.Trim(),
                MaskedCardNumber = maskedCard,
                PaymentStatus = "Completed",
                Notes = initialNotes,
                DonationDate = DateTime.UtcNow
            };

            _context.Donations.Add(donation);

            // Increment cause raised amount
            cause.RaisedAmount += model.Amount;

            await _context.SaveChangesAsync();

            // Auto-send formal donation receipt email via Brevo SMTP
            bool emailSent = false;
            try
            {
                await _emailSender.SendDonationReceiptAsync(
                    donation.DonorEmail,
                    donation.DonorName,
                    donation.TransactionId,
                    donation.Amount,
                    donationType,
                    cause.Title,
                    cause.Ngo?.Name ?? "NGEOS Foundation",
                    donation.DonationDate,
                    donation.CardType,
                    donation.MaskedCardNumber
                );
                emailSent = true;
                _logger.LogInformation("Donation receipt emailed successfully to {Email} for Txn {TxnId}", donation.DonorEmail, donation.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to email donation receipt to {Email}", donation.DonorEmail);
            }

            // Update Email status tag in donation record
            var finalEmailTag = emailSent ? "[Email:Sent]" : "[Email:Failed]";
            donation.Notes = string.IsNullOrWhiteSpace(userRawNote)
                ? $"[Type:{donationType}] {finalEmailTag}"
                : $"[Type:{donationType}] {finalEmailTag} {userRawNote}";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Thank you for your generous {donationType} contribution of ${model.Amount:N2} to '{cause.Title}'!";
            return RedirectToAction("Confirmation", new { transactionId = txnId });
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
                .FirstOrDefaultAsync(d => d.TransactionId == transactionId);

            if (donation == null)
            {
                return NotFound();
            }

            var receipt = new DonationReceiptViewModel
            {
                TransactionId = donation.TransactionId,
                CauseTitle = donation.Cause?.Title ?? "General Charity Relief",
                Category = donation.Cause?.Category ?? "Welfare",
                NgoName = donation.Cause?.Ngo?.Name ?? "NGEOS Foundation",
                DonorName = donation.DonorName,
                DonorEmail = donation.DonorEmail,
                DonorPhone = donation.DonorPhone,
                Amount = donation.Amount,
                CardType = donation.CardType,
                MaskedCardNumber = donation.MaskedCardNumber,
                DonationDate = donation.DonationDate,
                PaymentStatus = donation.PaymentStatus,
                DonationType = donation.DonationType,
                IsReceiptEmailSent = donation.IsReceiptEmailSent,
                Notes = donation.CleanNotes
            };

            return View(receipt);
        }
    }
}
