using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Models;

namespace NGEOS.Controllers
{
    public class CausesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CausesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? category, string? search)
        {
            var query = _context.Causes
                .Include(c => c.Ngo)
                .Where(c => c.IsActive);

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                query = query.Where(c => c.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Title.Contains(search) || c.ShortDescription.Contains(search) || c.Category.Contains(search));
            }

            var causes = await query.OrderByDescending(c => c.IsFeatured).ThenByDescending(c => c.CreatedAt).ToListAsync();

            ViewBag.Categories = new List<string> { "All", "Children", "Education", "Healthcare", "Disabled Persons", "Women", "Youth", "Elderly" };
            ViewBag.SelectedCategory = category ?? "All";
            ViewBag.SearchTerm = search;

            return View(causes);
        }

        public async Task<IActionResult> Details(int id)
        {
            var cause = await _context.Causes
                .Include(c => c.Ngo)
                .Include(c => c.Donations)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (cause == null)
            {
                return NotFound();
            }

            var recentDonations = await _context.Donations
                .Where(d => d.CauseId == id && d.PaymentStatus == "Completed")
                .OrderByDescending(d => d.DonationDate)
                .Take(5)
                .ToListAsync();

            var relatedCauses = await _context.Causes
                .Where(c => c.Id != id && c.IsActive && c.Category == cause.Category)
                .Take(3)
                .ToListAsync();

            ViewBag.RecentDonations = recentDonations;
            ViewBag.RelatedCauses = relatedCauses;

            return View(cause);
        }
    }
}
