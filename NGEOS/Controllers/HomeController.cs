using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Models;
using NGEOS.Services;
using NGEOS.ViewModels;

namespace NGEOS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Gather statistics
            var totalDonationsRaised = await _context.Causes.SumAsync(c => (decimal?)c.RaisedAmount) ?? 0;
            var totalLivesHelped = 12500;
            var totalCausesCount = await _context.Causes.CountAsync(c => c.IsActive);
            var totalNgosCount = await _context.NGOs.CountAsync(n => n.IsActive);
            var totalProgrammesCount = await _context.Programmes.CountAsync(p => p.IsActive);

            // Featured & Active Causes
            var featuredCauses = await _context.Causes
                .Include(c => c.Ngo)
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.IsFeatured)
                .ThenByDescending(c => c.CreatedAt)
                .Take(6)
                .ToListAsync();

            // Highlighted Featured Campaign for donation spotlight
            var highlightCampaign = await _context.Causes
                .Include(c => c.Ngo)
                .FirstOrDefaultAsync(c => c.IsFeatured && c.IsActive)
                ?? featuredCauses.FirstOrDefault();

            // Upcoming Programmes
            var upcomingProgrammes = await _context.Programmes
                .Include(p => p.Ngo)
                .Where(p => p.IsActive)
                .OrderBy(p => p.StartDate)
                .Take(3)
                .ToListAsync();

            // Partner NGOs
            var partnerNgos = await _context.NGOs
                .Where(n => n.IsActive)
                .OrderBy(n => n.Name)
                .Take(6)
                .ToListAsync();

            // Corporate Partners
            var partners = await _context.Partners
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            // Gallery Preview
            var galleryPreview = await _context.GalleryItems
                .OrderByDescending(g => g.CreatedAt)
                .Take(6)
                .ToListAsync();

            // Latest Events
            var latestEvents = await _context.Events
                .Where(e => e.IsUpcoming)
                .OrderBy(e => e.EventDate)
                .Take(3)
                .ToListAsync();

            ViewBag.TotalDonationsRaised = totalDonationsRaised;
            ViewBag.TotalLivesHelped = totalLivesHelped;
            ViewBag.TotalCausesCount = totalCausesCount;
            ViewBag.TotalNgosCount = totalNgosCount;
            ViewBag.TotalProgrammesCount = totalProgrammesCount;
            ViewBag.HighlightCampaign = highlightCampaign;
            ViewBag.UpcomingProgrammes = upcomingProgrammes;
            ViewBag.PartnerNgos = partnerNgos;
            ViewBag.Partners = partners;
            ViewBag.GalleryPreview = galleryPreview;
            ViewBag.LatestEvents = latestEvents;

            return View(featuredCauses);
        }

        [HttpGet]
        public async Task<IActionResult> Partners()
        {
            var partners = await _context.Partners
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            return View(partners);
        }

        [HttpGet]
        public async Task<IActionResult> HelpCentre(string? search, string? category)
        {
            var query = _context.FAQs.Where(f => f.IsActive);

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                query = query.Where(f => f.Category == category);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(f => f.Question.Contains(search) || f.Answer.Contains(search));
            }

            var faqs = await query.OrderBy(f => f.DisplayOrder).ToListAsync();
            ViewBag.Categories = await _context.FAQs.Where(f => f.IsActive).Select(f => f.Category).Distinct().ToListAsync();
            ViewBag.SelectedCategory = category ?? "All";
            ViewBag.SearchTerm = search;

            return View(faqs);
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactUsViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactUsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var contactMessage = new ContactMessage
            {
                Name = model.Name.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone?.Trim(),
                Subject = model.Subject.Trim(),
                Message = model.Message.Trim(),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.ContactMessages.Add(contactMessage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you for reaching out to NGEOS! Your message has been received and our team will respond shortly.";
            return RedirectToAction(nameof(Contact));
        }

        [HttpGet]
        public IActionResult RaiseQuery()
        {
            var model = new RaiseQueryRequestViewModel();
            if (User.Identity?.IsAuthenticated == true)
            {
                model.FullName = User.Identity.Name ?? "";
                model.Email = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RaiseQuery(RaiseQueryRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int parsedId))
                {
                    userId = parsedId;
                }
            }

            var query = new UserQuery
            {
                UserId = userId,
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Subject = model.Subject.Trim(),
                Category = model.Category,
                Message = model.Message.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.UserQueries.Add(query);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your query has been submitted successfully. You can track updates from your user dashboard or via email.";
            
            if (userId.HasValue)
            {
                return RedirectToAction("Queries", "User");
            }

            return RedirectToAction(nameof(HelpCentre));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubscribeNewsletter([FromForm] string email)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                         || Request.Headers["Accept"].ToString().Contains("application/json");

            if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Please provide a valid email address." });
                }
                TempData["ErrorMessage"] = "Please enter a valid email address.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }

            email = email.Trim().ToLowerInvariant();

            // Check if already subscribed
            var alreadySubscribed = await _context.ContactMessages
                .AnyAsync(m => m.Subject == "Newsletter Subscription" && m.Email.ToLower() == email);

            if (alreadySubscribed)
            {
                if (isAjax)
                {
                    return Json(new { success = true, message = "You are already subscribed to NGEOS updates!" });
                }
                TempData["SuccessMessage"] = "You are already subscribed to NGEOS updates!";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }

            var subscription = new ContactMessage
            {
                Name = "Newsletter Subscriber",
                Email = email,
                Subject = "Newsletter Subscription",
                Message = "Subscriber opted in for NGEOS newsletters, weekly welfare reports, and charity campaign notifications from footer.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.ContactMessages.Add(subscription);
            await _context.SaveChangesAsync();

            if (isAjax)
            {
                return Json(new { success = true, message = "Thank you! You have successfully subscribed to NGEOS updates." });
            }

            TempData["SuccessMessage"] = "Thank you! You have successfully subscribed to NGEOS updates.";
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
