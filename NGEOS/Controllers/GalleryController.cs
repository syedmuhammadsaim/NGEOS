using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;

namespace NGEOS.Controllers
{
    public class GalleryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GalleryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? category)
        {
            var query = _context.GalleryItems.AsQueryable();

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                query = query.Where(g => g.Category.ToLower() == category.ToLower());
            }

            var items = await query.OrderByDescending(g => g.CreatedAt).ToListAsync();

            ViewBag.Categories = new List<string> { "All", "Education", "Healthcare", "Children", "Relief", "Events" };
            ViewBag.SelectedCategory = category ?? "All";

            return View(items);
        }
    }
}
