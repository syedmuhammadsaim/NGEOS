using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Models;
using NGEOS.Models.ApiDtos;

namespace NGEOS.Controllers.Api
{
    [ApiController]
    [Route("api/v1/programmes")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ProgrammesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProgrammesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<ProgrammeDto>>> GetProgrammes()
        {
            var programmes = await _context.Programmes
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProgrammeDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Category = p.Category,
                    ShortDescription = p.ShortDescription,
                    FullDescription = p.FullDescription,
                    ImageUrl = p.ImageUrl,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Location = p.Location,
                    TargetBeneficiaries = p.TargetBeneficiaries,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return Ok(programmes);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<ActionResult<ProgrammeDto>> CreateProgramme([FromBody] ProgrammeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required.");

            var programme = new Programme
            {
                Title = dto.Title,
                Category = dto.Category ?? "General Welfare",
                ShortDescription = dto.ShortDescription ?? dto.Title,
                FullDescription = dto.FullDescription ?? dto.Title,
                ImageUrl = dto.ImageUrl ?? "https://images.unsplash.com/photo-1542810634-71277d95dcbb?w=800",
                StartDate = dto.StartDate ?? DateTime.UtcNow,
                EndDate = dto.EndDate,
                Location = dto.Location ?? "Global / Pakistan",
                TargetBeneficiaries = dto.TargetBeneficiaries > 0 ? dto.TargetBeneficiaries : 500,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Programmes.Add(programme);
            await _context.SaveChangesAsync();

            dto.Id = programme.Id;
            return CreatedAtAction(nameof(GetProgrammes), new { id = programme.Id }, dto);
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> UpdateProgramme(int id, [FromBody] ProgrammeDto dto)
        {
            var programme = await _context.Programmes.FindAsync(id);
            if (programme == null) return NotFound();

            programme.Title = dto.Title;
            programme.Category = dto.Category ?? programme.Category;
            programme.ShortDescription = dto.ShortDescription ?? programme.ShortDescription;
            programme.FullDescription = dto.FullDescription ?? programme.FullDescription;
            programme.ImageUrl = dto.ImageUrl ?? programme.ImageUrl;
            programme.StartDate = dto.StartDate ?? programme.StartDate;
            programme.EndDate = dto.EndDate;
            programme.Location = dto.Location ?? programme.Location;
            programme.TargetBeneficiaries = dto.TargetBeneficiaries;
            programme.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> DeleteProgramme(int id)
        {
            var programme = await _context.Programmes.FindAsync(id);
            if (programme == null) return NotFound();

            programme.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
