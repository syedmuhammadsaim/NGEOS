using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Models;

namespace NGEOS.Controllers
{
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AboutController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return await ReadAboutUs();
        }

        public async Task<IActionResult> WhatWeDo()
        {
            var page = await _context.AboutPages.FirstOrDefaultAsync(p => p.Slug == "what-we-do");
            return View("PageDetail", page ?? GetFallbackPage("what-we-do", "What We Do"));
        }

        public async Task<IActionResult> OurMission()
        {
            var page = await _context.AboutPages.FirstOrDefaultAsync(p => p.Slug == "our-mission");
            return View("PageDetail", page ?? GetFallbackPage("our-mission", "Our Mission & Vision"));
        }

        public async Task<IActionResult> OurTeam()
        {
            var page = await _context.AboutPages.FirstOrDefaultAsync(p => p.Slug == "our-team");
            var teamMembers = await _context.TeamMembers
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            ViewBag.AboutPage = page ?? GetFallbackPage("our-team", "Our Leadership & Humanitarian Team");
            return View(teamMembers);
        }

        public async Task<IActionResult> CareerWithUs()
        {
            var page = await _context.AboutPages.FirstOrDefaultAsync(p => p.Slug == "career-with-us");
            var jobs = await _context.JobOpportunities
                .Where(j => j.IsActive)
                .OrderByDescending(j => j.PostedDate)
                .ToListAsync();

            ViewBag.AboutPage = page ?? GetFallbackPage("career-with-us", "Careers With NGEOS");
            return View(jobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyJob(int jobId, string fullName, string email, string phone, string message)
        {
            TempData["SuccessMessage"] = $"Thank you {fullName}! Your job application has been submitted to NGEOS Human Resources. We will review your profile and contact you soon.";
            return RedirectToAction(nameof(CareerWithUs));
        }

        public async Task<IActionResult> OurAchievements()
        {
            var page = await _context.AboutPages.FirstOrDefaultAsync(p => p.Slug == "our-achievements");
            return View("PageDetail", page ?? GetFallbackPage("our-achievements", "Our Achievements"));
        }

        public async Task<IActionResult> OurSupporters()
        {
            var page = await _context.AboutPages.FirstOrDefaultAsync(p => p.Slug == "our-supporters");
            var partners = await _context.Partners
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            ViewBag.AboutPage = page ?? GetFallbackPage("our-supporters", "Our Generous Supporters & Partners");
            return View(partners);
        }

        public async Task<IActionResult> ReadAboutUs()
        {
            var page = await _context.AboutPages.FirstOrDefaultAsync(p => p.Slug == "read-about-us");
            return View("PageDetail", page ?? GetFallbackPage("read-about-us", "Read About NGEOS"));
        }

        private AboutPage GetFallbackPage(string slug, string title)
        {
            return new AboutPage
            {
                Slug = slug,
                Title = title,
                Subtitle = "Empowering Vulnerable Lives Worldwide",
                ContentHtml = "<p>NGEOS is dedicated to building sustainable welfare programs for children, education, health, women, and the elderly.</p>",
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}
