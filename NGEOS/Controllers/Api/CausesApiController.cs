using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Hubs;
using NGEOS.Models;
using NGEOS.Models.ApiDtos;

namespace NGEOS.Controllers.Api
{
    [ApiController]
    [Route("api/v1/causes")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CausesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NgeosRealtimeHub> _hubContext;

        public CausesApiController(ApplicationDbContext context, IHubContext<NgeosRealtimeHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<ActionResult<List<CauseDto>>> GetCauses()
        {
            var causes = await _context.Causes
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CauseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    ShortDescription = c.ShortDescription,
                    FullDescription = c.FullDescription,
                    Category = c.Category,
                    TargetAmount = c.TargetAmount,
                    RaisedAmount = c.RaisedAmount,
                    ImageUrl = c.ImageUrl,
                    IsFeatured = c.IsFeatured,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(causes);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<ActionResult<CauseDto>> CreateCause([FromBody] CauseDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required.");

            var cause = new Cause
            {
                Title = dto.Title,
                ShortDescription = dto.ShortDescription ?? dto.Title,
                FullDescription = dto.FullDescription ?? dto.Title,
                Category = dto.Category ?? "General",
                TargetAmount = dto.TargetAmount,
                RaisedAmount = 0,
                ImageUrl = dto.ImageUrl ?? "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=800",
                IsFeatured = dto.IsFeatured,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Causes.Add(cause);
            await _context.SaveChangesAsync();

            dto.Id = cause.Id;
            dto.CreatedAt = cause.CreatedAt;

            // Notify real-time clients
            await _hubContext.Clients.All.SendAsync("ReceiveCauseUpdate", dto);

            return CreatedAtAction(nameof(GetCauses), new { id = cause.Id }, dto);
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> UpdateCause(int id, [FromBody] CauseDto dto)
        {
            var cause = await _context.Causes.FindAsync(id);
            if (cause == null) return NotFound();

            cause.Title = dto.Title;
            cause.ShortDescription = dto.ShortDescription ?? cause.ShortDescription;
            cause.FullDescription = dto.FullDescription ?? cause.FullDescription;
            cause.Category = dto.Category ?? cause.Category;
            cause.TargetAmount = dto.TargetAmount;
            cause.ImageUrl = dto.ImageUrl ?? cause.ImageUrl;
            cause.IsFeatured = dto.IsFeatured;
            cause.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveCauseUpdate", dto);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> DeleteCause(int id)
        {
            var cause = await _context.Causes.FindAsync(id);
            if (cause == null) return NotFound();

            cause.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
