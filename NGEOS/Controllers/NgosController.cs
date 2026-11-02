using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;

namespace NGEOS.Controllers
{
    public class NgosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NgosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? focus)
        {
            var query = _context.NGOs
                .Include(n => n.Causes)
                .Include(n => n.Programmes)
                .Where(n => n.IsActive);

            if (!string.IsNullOrEmpty(focus) && focus != "All")
            {
                query = query.Where(n => n.FocusArea.Contains(focus));
            }

            var ngos = await query.OrderBy(n => n.Name).ToListAsync();

            ViewBag.FocusAreas = new List<string> { "All", "Children", "Education", "Healthcare", "Disabled Persons", "Women", "Elderly" };
            ViewBag.SelectedFocus = focus ?? "All";

            return View(ngos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var ngo = await _context.NGOs
                .Include(n => n.Causes.Where(c => c.IsActive))
                .Include(n => n.Programmes.Where(p => p.IsActive))
                .FirstOrDefaultAsync(n => n.Id == id && n.IsActive);

            if (ngo == null)
            {
                return NotFound();
            }

            return View(ngo);
        }
    }
}
