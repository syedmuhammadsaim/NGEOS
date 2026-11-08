using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Models;
using NGEOS.Services;
using NGEOS.ViewModels;

namespace NGEOS.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<UserController> _logger;

        public UserController(
            ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment,
            IEmailSender emailSender,
            ILogger<UserController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _emailSender = emailSender;
            _logger = logger;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int userId))
            {
                return await _context.Users.FindAsync(userId);
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var userEmail = (user.Email ?? string.Empty).Trim().ToLower();

                var donationsQuery = _context.Donations.Include(d => d.Cause).AsQueryable();
                if (user.Role != "Admin")
                {
                    donationsQuery = donationsQuery.Where(d => d.UserId == user.Id || (!string.IsNullOrEmpty(d.DonorEmail) && d.DonorEmail.ToLower() == userEmail));
                }
                var donations = await donationsQuery.OrderByDescending(d => d.DonationDate).ToListAsync();

                var interests = await _context.ProgrammeInterests
                    .Include(pi => pi.Programme)
                    .Where(pi => pi.UserId == user.Id || (!string.IsNullOrEmpty(pi.Email) && pi.Email.ToLower() == userEmail))
                    .OrderByDescending(pi => pi.SubmittedAt)
                    .ToListAsync();

                var queries = await _context.UserQueries
                    .Where(q => q.UserId == user.Id || (!string.IsNullOrEmpty(q.Email) && q.Email.ToLower() == userEmail))
                    .OrderByDescending(q => q.CreatedAt)
                    .ToListAsync();

                var invitations = await _context.FriendInvitations
                    .Include(fi => fi.Cause)
                    .Where(fi => fi.UserId == user.Id || (!string.IsNullOrEmpty(fi.SenderEmail) && fi.SenderEmail.ToLower() == userEmail))
                    .OrderByDescending(fi => fi.SentAt)
                    .ToListAsync();

                var viewModel = new UserDashboardViewModel
                {
                    User = user,
                    TotalDonated = donations.Where(d => d.PaymentStatus == "Completed").Sum(d => d.Amount),
                    CausesSupportedCount = donations.Select(d => d.CauseId).Distinct().Count(),
                    ProgrammesJoinedCount = interests.Count,
                    QueriesCount = queries.Count,
                    RecentDonations = donations.Take(5).ToList(),
                    MyProgrammeInterests = interests.Take(5).ToList(),
                    MyQueries = queries.Take(5).ToList(),
                    MyInvitations = invitations.Take(5).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading User Dashboard for user {UserId}", user.Id);
                var fallbackModel = new UserDashboardViewModel
                {
                    User = user,
                    TotalDonated = 0,
                    CausesSupportedCount = 0,
                    ProgrammesJoinedCount = 0,
                    QueriesCount = 0
                };
                TempData["ErrorMessage"] = "Some dashboard modules are syncing with the database. Showing overview.";
                return View(fallbackModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Donations()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var donationsQuery = _context.Donations.Include(d => d.Cause).AsQueryable();
            if (user.Role != "Admin")
            {
                donationsQuery = donationsQuery.Where(d => d.UserId == user.Id || d.DonorEmail.ToLower() == user.Email.ToLower());
            }
            var donations = await donationsQuery.OrderByDescending(d => d.DonationDate).ToListAsync();

            return View(donations);
        }

        [HttpGet]
        public async Task<IActionResult> Programmes()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var interests = await _context.ProgrammeInterests
                .Include(pi => pi.Programme)
                .ThenInclude(p => p!.Ngo)
                .Where(pi => pi.UserId == user.Id || pi.Email.ToLower() == user.Email.ToLower())
                .OrderByDescending(pi => pi.SubmittedAt)
                .ToListAsync();

            return View(interests);
        }

        [HttpGet]
        public async Task<IActionResult> Queries()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var queries = await _context.UserQueries
                .Where(q => q.UserId == user.Id || q.Email.ToLower() == user.Email.ToLower())
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return View(queries);
        }

        [HttpGet]
        public async Task<IActionResult> InviteFriends(int? causeId)
        {
            var user = await GetCurrentUserAsync();
            var causes = await _context.Causes.Where(c => c.IsActive).ToListAsync();

            var model = new InviteFriendViewModel
            {
                SenderName = user?.FullName ?? "",
                SenderEmail = user?.Email ?? "",
                CauseId = causeId,
                Causes = causes
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteFriends(InviteFriendViewModel model)
        {
            var causes = await _context.Causes.Where(c => c.IsActive).ToListAsync();
            model.Causes = causes;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await GetCurrentUserAsync();

            var invitation = new FriendInvitation
            {
                UserId = user?.Id,
                SenderName = model.SenderName.Trim(),
                SenderEmail = model.SenderEmail.Trim(),
                FriendEmail = model.FriendEmail.Trim(),
                CauseId = model.CauseId,
                PersonalMessage = model.PersonalMessage?.Trim(),
                SentAt = DateTime.UtcNow
            };

            _context.FriendInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            // Fetch Cause details if selected
            Cause? cause = null;
            if (model.CauseId.HasValue && model.CauseId.Value > 0)
            {
                cause = await _context.Causes.FirstOrDefaultAsync(c => c.Id == model.CauseId.Value);
            }

            // Construct link to cause or causes list
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var exploreUrl = cause != null
                ? (Url.Action("Details", "Causes", new { id = cause.Id }, Request.Scheme) ?? $"{baseUrl}/Causes/Details/{cause.Id}")
                : (Url.Action("Index", "Causes", null, Request.Scheme) ?? $"{baseUrl}/Causes");

            // Format cause image URL if present
            string? causeImageUrl = null;
            if (cause != null && !string.IsNullOrWhiteSpace(cause.ImageUrl))
            {
                causeImageUrl = cause.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? cause.ImageUrl
                    : $"{baseUrl}{cause.ImageUrl}";
            }

            // Dispatch actual invitation email via SMTP
            try
            {
                await _emailSender.SendFriendInvitationAsync(
                    toEmail: model.FriendEmail.Trim(),
                    senderName: model.SenderName.Trim(),
                    senderEmail: model.SenderEmail.Trim(),
                    personalMessage: model.PersonalMessage?.Trim() ?? string.Empty,
                    causeTitle: cause?.Title,
                    causeCategory: cause?.Category,
                    causeDescription: cause?.ShortDescription ?? cause?.FullDescription,
                    causeImageUrl: causeImageUrl,
                    exploreUrl: exploreUrl
                );

                _logger.LogInformation("Invitation email sent successfully to {FriendEmail} by {SenderEmail}", model.FriendEmail, model.SenderEmail);
                TempData["SuccessMessage"] = $"Your invitation email has been sent successfully to {model.FriendEmail}! Thank you for spreading the word about NGEOS.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send friend invitation email to {FriendEmail}", model.FriendEmail);
                TempData["ErrorMessage"] = $"Your invitation record was saved, but sending the email to '{model.FriendEmail}' failed: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User updatedUser, IFormFile? profileImage, bool removeProfileImage = false)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            // 1. Handle remove profile image request
            if (removeProfileImage)
            {
                if (!string.IsNullOrEmpty(user.ProfileImageUrl) && user.ProfileImageUrl.StartsWith("/uploads/profiles/"))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfileImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try { System.IO.File.Delete(oldFilePath); } catch { /* ignore */ }
                    }
                }
                user.ProfileImageUrl = null;
            }
            // 2. Handle new profile image upload
            else if (profileImage != null && profileImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Only JPG, JPEG, PNG, or WEBP image files are allowed.";
                    return View(user);
                }

                if (profileImage.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Profile image file size must not exceed 5 MB.";
                    return View(user);
                }

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete previous local file if any
                if (!string.IsNullOrEmpty(user.ProfileImageUrl) && user.ProfileImageUrl.StartsWith("/uploads/profiles/"))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfileImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try { System.IO.File.Delete(oldFilePath); } catch { /* ignore */ }
                    }
                }

                // Generate safe unique filename
                var uniqueFileName = $"user_{user.Id}_{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                user.ProfileImageUrl = $"/uploads/profiles/{uniqueFileName}";
            }

            user.FullName = updatedUser.FullName.Trim();
            user.Phone = updatedUser.Phone?.Trim();
            user.Address = updatedUser.Address?.Trim();
            user.Profession = updatedUser.Profession?.Trim();
            user.Organization = updatedUser.Organization?.Trim();

            await _context.SaveChangesAsync();
            await RefreshUserClaimsAsync(user);

            TempData["SuccessMessage"] = "Your profile information and photo have been updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        private async Task RefreshUserClaimsAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Profession", user.Profession ?? ""),
                new Claim("Organization", user.Organization ?? ""),
                new Claim("ProfileImageUrl", user.ProfileImageUrl ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));
        }
    }
}
