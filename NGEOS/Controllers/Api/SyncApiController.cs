using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Models;
using NGEOS.Models.ApiDtos;

namespace NGEOS.Controllers.Api
{
    [ApiController]
    [Route("api/v1/sync")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class SyncApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SyncApiController> _logger;

        public SyncApiController(ApplicationDbContext context, ILogger<SyncApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("pull")]
        public async Task<ActionResult<SyncPullResponseDto>> PullSyncData([FromQuery] DateTime? since = null)
        {
            var isUserAdmin = User.IsInRole("Admin");
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value?.ToLower() ?? "";

            var causes = await _context.Causes
                .Where(c => c.IsActive && (!since.HasValue || c.CreatedAt >= since.Value))
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

            var programmes = await _context.Programmes
                .Where(p => p.IsActive && (!since.HasValue || p.CreatedAt >= since.Value))
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

            var donationsQuery = _context.Donations.Include(d => d.Cause).AsQueryable();
            if (!isUserAdmin)
            {
                donationsQuery = donationsQuery.Where(d => d.UserId == currentUserId || (d.DonorEmail != null && d.DonorEmail.ToLower() == userEmail));
            }
            if (since.HasValue)
            {
                donationsQuery = donationsQuery.Where(d => d.DonationDate >= since.Value);
            }

            var donations = await donationsQuery
                .OrderByDescending(d => d.DonationDate)
                .Take(150)
                .Select(d => new RecentTransactionDto
                {
                    Id = d.Id,
                    TransactionId = d.TransactionId,
                    DonorName = d.DonorName,
                    DonorEmail = d.DonorEmail,
                    Amount = d.Amount,
                    Currency = d.Currency,
                    PaymentGateway = d.PaymentGateway,
                    PaymentStatus = d.PaymentStatus,
                    CauseTitle = d.Cause != null ? d.Cause.Title : "Charity Cause",
                    DonationDate = d.DonationDate
                })
                .ToListAsync();

            var queriesQuery = _context.UserQueries.AsQueryable();
            if (!isUserAdmin)
            {
                queriesQuery = queriesQuery.Where(q => q.UserId == currentUserId || (q.Email != null && q.Email.ToLower() == userEmail));
            }
            if (since.HasValue)
            {
                queriesQuery = queriesQuery.Where(q => q.CreatedAt >= since.Value || (q.RepliedAt.HasValue && q.RepliedAt.Value >= since.Value));
            }

            var queries = await queriesQuery
                .OrderByDescending(q => q.CreatedAt)
                .Take(100)
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

            return Ok(new SyncPullResponseDto
            {
                ServerTimeUtc = DateTime.UtcNow,
                Causes = causes,
                Programmes = programmes,
                Donations = donations,
                Queries = queries
            });
        }

        [HttpPost("push")]
        public async Task<ActionResult<List<SyncPushResultDto>>> PushSyncOperations([FromBody] SyncPushRequestDto request)
        {
            var results = new List<SyncPushResultDto>();
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "User";
            var isAdmin = User.IsInRole("Admin");

            foreach (var op in request.Operations)
            {
                try
                {
                    if (op.EntityType == "Query" && op.ActionType == "Create")
                    {
                        var dto = JsonSerializer.Deserialize<UserQueryDto>(op.JsonPayload);
                        if (dto != null)
                        {
                            var newQuery = new UserQuery
                            {
                                UserId = currentUserId > 0 ? currentUserId : null,
                                FullName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName : userName,
                                Email = !string.IsNullOrWhiteSpace(dto.Email) ? dto.Email : userEmail,
                                Subject = dto.Subject ?? "Offline Sync Query",
                                Category = dto.Category ?? "General Inquiry",
                                Message = dto.Message ?? "",
                                Status = "Pending",
                                CreatedAt = op.CreatedAtUtc != default ? op.CreatedAtUtc : DateTime.UtcNow
                            };
                            _context.UserQueries.Add(newQuery);
                            await _context.SaveChangesAsync();

                            results.Add(new SyncPushResultDto
                            {
                                OperationId = op.OperationId,
                                Success = true,
                                Message = "Query synchronized successfully.",
                                ServerEntityId = newQuery.Id
                            });
                        }
                    }
                    else if (op.EntityType == "Cause" && (op.ActionType == "Create" || op.ActionType == "Update") && isAdmin)
                    {
                        var dto = JsonSerializer.Deserialize<CauseDto>(op.JsonPayload);
                        if (dto != null)
                        {
                            if (op.ActionType == "Create")
                            {
                                var cause = new Cause
                                {
                                    Title = dto.Title,
                                    ShortDescription = dto.ShortDescription,
                                    FullDescription = dto.FullDescription,
                                    Category = dto.Category,
                                    TargetAmount = dto.TargetAmount,
                                    RaisedAmount = dto.RaisedAmount,
                                    ImageUrl = dto.ImageUrl ?? "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=800",
                                    IsFeatured = dto.IsFeatured,
                                    IsActive = true,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _context.Causes.Add(cause);
                                await _context.SaveChangesAsync();

                                results.Add(new SyncPushResultDto
                                {
                                    OperationId = op.OperationId,
                                    Success = true,
                                    Message = "Cause created via sync.",
                                    ServerEntityId = cause.Id
                                });
                            }
                            else
                            {
                                var cause = await _context.Causes.FindAsync(dto.Id);
                                if (cause != null)
                                {
                                    cause.Title = dto.Title;
                                    cause.ShortDescription = dto.ShortDescription;
                                    cause.FullDescription = dto.FullDescription;
                                    cause.Category = dto.Category;
                                    cause.TargetAmount = dto.TargetAmount;
                                    cause.IsActive = dto.IsActive;
                                    await _context.SaveChangesAsync();

                                    results.Add(new SyncPushResultDto
                                    {
                                        OperationId = op.OperationId,
                                        Success = true,
                                        Message = "Cause updated via sync.",
                                        ServerEntityId = cause.Id
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        results.Add(new SyncPushResultDto
                        {
                            OperationId = op.OperationId,
                            Success = false,
                            Message = $"Unsupported or unauthorized operation for {op.EntityType}:{op.ActionType}."
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync operation {OperationId}", op.OperationId);
                    results.Add(new SyncPushResultDto
                    {
                        OperationId = op.OperationId,
                        Success = false,
                        Message = $"Sync error: {ex.Message}"
                    });
                }
            }

            return Ok(results);
        }
    }
}
