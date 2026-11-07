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
    [Route("api/v1/donations")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class DonationsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NgeosRealtimeHub> _hubContext;
        private readonly ILogger<DonationsApiController> _logger;

        public DonationsApiController(ApplicationDbContext context, IHubContext<NgeosRealtimeHub> hubContext, ILogger<DonationsApiController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<RecentTransactionDto>>> GetDonations([FromQuery] string? status = null, [FromQuery] string? search = null)
        {
            var isUserAdmin = User.IsInRole("Admin");
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value?.ToLower() ?? "";

            var query = _context.Donations.Include(d => d.Cause).AsQueryable();

            if (!isUserAdmin)
            {
                query = query.Where(d => d.UserId == currentUserId || (d.DonorEmail != null && d.DonorEmail.ToLower() == userEmail));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(d => d.PaymentStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d => d.DonorName.Contains(search) || d.TransactionId.Contains(search) || (d.Cause != null && d.Cause.Title.Contains(search)));
            }

            var donations = await query
                .OrderByDescending(d => d.DonationDate)
                .Take(100)
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
                    CauseTitle = d.Cause != null ? d.Cause.Title : "Charity Relief",
                    DonationDate = d.DonationDate
                })
                .ToListAsync();

            return Ok(donations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RecentTransactionDto>> GetDonationById(int id)
        {
            var isUserAdmin = User.IsInRole("Admin");
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value?.ToLower() ?? "";

            var d = await _context.Donations.Include(x => x.Cause).FirstOrDefaultAsync(x => x.Id == id);
            if (d == null) return NotFound();

            if (!isUserAdmin && d.UserId != currentUserId && d.DonorEmail?.ToLower() != userEmail)
            {
                return Forbid();
            }

            return Ok(new RecentTransactionDto
            {
                Id = d.Id,
                TransactionId = d.TransactionId,
                DonorName = d.DonorName,
                DonorEmail = d.DonorEmail,
                Amount = d.Amount,
                Currency = d.Currency,
                PaymentGateway = d.PaymentGateway,
                PaymentStatus = d.PaymentStatus,
                CauseTitle = d.Cause != null ? d.Cause.Title : "Charity Relief",
                DonationDate = d.DonationDate
            });
        }
    }
}
