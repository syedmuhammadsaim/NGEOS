using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;

namespace NGEOS.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var upcomingEvents = await _context.Events
                .Where(e => e.IsUpcoming)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            var pastEvents = await _context.Events
                .Where(e => !e.IsUpcoming)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            ViewBag.PastEvents = pastEvents;
            return View(upcomingEvents);
        }
    }
}
