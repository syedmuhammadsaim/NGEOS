using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Models;
using NGEOS.Models.ApiDtos;

namespace NGEOS.Controllers.Api
{
    [ApiController]
    [Route("api/v1/queries")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class QueriesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public QueriesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserQueryDto>>> GetQueries([FromQuery] string? status = null)
        {
            var isUserAdmin = User.IsInRole("Admin");
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value?.ToLower() ?? "";

            var query = _context.UserQueries.AsQueryable();

            if (!isUserAdmin)
            {
                query = query.Where(q => q.UserId == currentUserId || (q.Email != null && q.Email.ToLower() == userEmail));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(q => q.Status == status);
            }

            var list = await query
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => new UserQueryDto
                {
                    Id = q.Id,
                    UserId = q.UserId,
                    FullName = q.FullName,
                    Email = q.Email,
                    Subject = q.Subject,
                    Category = q.Category,
                    Message = q.Message,
                    AdminReply = q.AdminReply,
                    Status = q.Status,
                    CreatedAt = q.CreatedAt,
                    RepliedAt = q.RepliedAt
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult<UserQueryDto>> CreateQuery([FromBody] UserQueryDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? dto.Email;
            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? dto.FullName;

            var userQuery = new UserQuery
            {
                UserId = currentUserId > 0 ? currentUserId : null,
                FullName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName : userName,
                Email = !string.IsNullOrWhiteSpace(dto.Email) ? dto.Email : userEmail,
                Subject = dto.Subject ?? "Support Request",
                Category = dto.Category ?? "General Inquiry",
                Message = dto.Message ?? "",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.UserQueries.Add(userQuery);
            await _context.SaveChangesAsync();

            dto.Id = userQuery.Id;
            dto.CreatedAt = userQuery.CreatedAt;
            dto.Status = userQuery.Status;

            return CreatedAtAction(nameof(GetQueries), new { id = userQuery.Id }, dto);
        }

        [HttpPut("{id}/reply")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> ReplyQuery(int id, [FromBody] string reply)
        {
            var q = await _context.UserQueries.FindAsync(id);
            if (q == null) return NotFound();

            q.AdminReply = reply;
            q.Status = "Replied";
            q.RepliedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
