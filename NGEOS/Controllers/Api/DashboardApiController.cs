using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Models.ApiDtos;

namespace NGEOS.Controllers.Api
{
    [ApiController]
    [Route("api/v1/dashboard")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class DashboardApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("admin")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<ActionResult<AdminDashboardDto>> GetAdminDashboard()
        {
            var completedDonations = await _context.Donations
                .Include(d => d.Cause)
                .Where(d => d.PaymentStatus == "Completed")
                .ToListAsync();

            var totalDonations = completedDonations.Sum(d => d.Amount);
            var totalDonors = await _context.Donations
                .Select(d => d.DonorEmail.ToLower())
                .Distinct()
                .CountAsync();

            var totalCampaigns = await _context.Causes.CountAsync();
            var totalProjects = await _context.Programmes.CountAsync();
            var totalBeneficiaries = await _context.Programmes.SumAsync(p => (long)p.TargetBeneficiaries);

            var pendingRequests = await _context.UserQueries.CountAsync(q => q.Status == "Pending");
            var approvedRequests = await _context.ProgrammeInterests.CountAsync(pi => pi.Status == "Approved");
            var rejectedRequests = await _context.ProgrammeInterests.CountAsync(pi => pi.Status == "Rejected");

            // Monthly donation chart (last 6 months)
            var now = DateTime.UtcNow;
            var monthlyChart = new List<MonthlyDonationChartItem>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var monthDonations = completedDonations
                    .Where(d => d.DonationDate >= monthStart && d.DonationDate < monthEnd)
                    .ToList();

                monthlyChart.Add(new MonthlyDonationChartItem
                {
                    Month = monthDate.ToString("MMM yyyy"),
                    Amount = monthDonations.Sum(d => d.Amount),
                    Count = monthDonations.Count
                });
            }

            // Category breakdown
            var categoryStats = completedDonations
                .GroupBy(d => d.Cause?.Category ?? "General")
                .Select(g => new CategoryDonationItem
                {
                    Category = g.Key,
                    Amount = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            // Recent 10 transactions
            var recentTransactions = await _context.Donations
                .Include(d => d.Cause)
                .OrderByDescending(d => d.DonationDate)
                .Take(10)
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

            // Recent activities
            var recentActivities = new List<RecentActivityDto>();
            foreach (var tx in recentTransactions.Take(5))
            {
                recentActivities.Add(new RecentActivityDto
                {
                    Title = $"Donation: Rs {tx.Amount:N0}",
                    Description = $"{tx.DonorName} donated to {tx.CauseTitle} via {tx.PaymentGateway}",
                    ActivityType = "Donation",
                    Timestamp = tx.DonationDate
                });
            }

            return Ok(new AdminDashboardDto
            {
                TotalDonations = totalDonations,
                TotalDonors = totalDonors,
                TotalCampaigns = totalCampaigns,
                TotalBeneficiaries = (int)Math.Min(totalBeneficiaries, int.MaxValue),
                TotalProjects = totalProjects,
                PendingRequests = pendingRequests,
                ApprovedRequests = approvedRequests,
                RejectedRequests = rejectedRequests,
                MonthlyChart = monthlyChart,
                CategoryStats = categoryStats,
                RecentTransactions = recentTransactions,
                RecentActivities = recentActivities
            });
        }

        [HttpGet("user")]
        public async Task<ActionResult<UserDashboardDto>> GetUserDashboard()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var userEmail = user.Email.ToLower();

            var userDonations = await _context.Donations
                .Include(d => d.Cause)
                .Where(d => d.UserId == userId || (d.DonorEmail != null && d.DonorEmail.ToLower() == userEmail))
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            var completedTotal = userDonations.Where(d => d.PaymentStatus == "Completed").Sum(d => d.Amount);
            var causesCount = userDonations.Select(d => d.CauseId).Distinct().Count();

            var myProgrammes = await _context.ProgrammeInterests
                .Include(pi => pi.Programme)
                .Where(pi => pi.UserId == userId || (pi.Email != null && pi.Email.ToLower() == userEmail))
                .OrderByDescending(pi => pi.SubmittedAt)
                .Take(5)
                .Select(pi => new ProgrammeInterestDto
                {
                    Id = pi.Id,
                    ProgrammeId = pi.ProgrammeId,
                    ProgrammeTitle = pi.Programme != null ? pi.Programme.Title : "Programme",
                    InterestType = pi.InterestType,
                    Status = pi.Status,
                    SubmittedAt = pi.SubmittedAt
                })
                .ToListAsync();

            var myQueries = await _context.UserQueries
                .Where(q => q.UserId == userId || (q.Email != null && q.Email.ToLower() == userEmail))
                .OrderByDescending(q => q.CreatedAt)
                .Take(5)
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

            return Ok(new UserDashboardDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                TotalDonated = completedTotal,
                CausesSupportedCount = causesCount,
                ProgrammesJoinedCount = myProgrammes.Count,
                QueriesCount = myQueries.Count,
                RecentDonations = userDonations.Take(5).Select(d => new RecentTransactionDto
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
                }).ToList(),
                MyProgrammes = myProgrammes,
                MyQueries = myQueries
            });
        }
    }
}
