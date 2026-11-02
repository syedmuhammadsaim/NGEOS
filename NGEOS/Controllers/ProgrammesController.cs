using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Models;
using NGEOS.ViewModels;

namespace NGEOS.Controllers
{
    public class ProgrammesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProgrammesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? category)
        {
            var query = _context.Programmes
                .Include(p => p.Ngo)
                .Include(p => p.Interests)
                .Where(p => p.IsActive);

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                query = query.Where(p => p.Category.ToLower() == category.ToLower());
            }

            var programmes = await query.OrderBy(p => p.StartDate).ToListAsync();

            ViewBag.Categories = new List<string> { "All", "Education", "Health Care", "Privileged children (Specially challenged)", "Women Empowerment", "Elderly Support" };
            ViewBag.SelectedCategory = category ?? "All";

            return View(programmes);
        }

        public async Task<IActionResult> Details(int id)
        {
            var programme = await _context.Programmes
                .Include(p => p.Ngo)
                .Include(p => p.Interests)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (programme == null)
            {
                return NotFound();
            }

            var interestModel = new ProgrammeInterestRequestViewModel
            {
                ProgrammeId = programme.Id,
                ProgrammeTitle = programme.Title
            };

            if (User.Identity?.IsAuthenticated == true)
            {
                interestModel.FullName = User.Identity.Name ?? "";
                interestModel.Email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "";
                interestModel.Occupation = User.Claims.FirstOrDefault(c => c.Type == "Profession")?.Value ?? "";
            }

            ViewBag.InterestModel = interestModel;
            return View(programme);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShowInterest(ProgrammeInterestRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill out all required fields to register your interest.";
                return RedirectToAction(nameof(Details), new { id = model.ProgrammeId });
            }

            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int parsedId))
                {
                    userId = parsedId;
                }
            }

            var interest = new ProgrammeInterest
            {
                ProgrammeId = model.ProgrammeId,
                UserId = userId,
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                Occupation = model.Occupation?.Trim(),
                InterestType = model.InterestType,
                Message = model.Message?.Trim(),
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow
            };

            _context.ProgrammeInterests.Add(interest);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you for showing interest in this programme! Our volunteering and event team will contact you shortly.";
            
            if (userId.HasValue)
            {
                return RedirectToAction("Programmes", "User");
            }

            return RedirectToAction(nameof(Details), new { id = model.ProgrammeId });
        }
    }
}
