using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Hubs;
using NGEOS.Models;
using NGEOS.Services;

namespace NGEOS.Controllers.Api
{
    [ApiController]
    [Route("api/agent/internal")]
    public class AgentBridgeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NgeosRealtimeHub> _hubContext;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AgentBridgeApiController> _logger;

        private const string DefaultInternalKey = "NGEOS_AI_INTERNAL_SHARED_KEY_2026_SECURE_TOKEN_32X";

        public AgentBridgeApiController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IHubContext<NgeosRealtimeHub> hubContext,
            IEmailSender emailSender,
            ILogger<AgentBridgeApiController> logger)
        {
            _context = context;
            _configuration = configuration;
            _hubContext = hubContext;
            _emailSender = emailSender;
            _logger = logger;
        }

        private bool ValidateInternalKey()
        {
            if (!Request.Headers.TryGetValue("X-NGEOS-Internal-Key", out var keyValues))
            {
                return false;
            }

            var expectedKey = _configuration["AI_SERVICE_SECRET_KEY"] ?? DefaultInternalKey;
            return string.Equals(keyValues.ToString().Trim(), expectedKey.Trim(), StringComparison.Ordinal);
        }

        [HttpGet("analytics/donations")]
        public async Task<IActionResult> GetDonationAnalytics()
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });

            var now = DateTime.UtcNow;
            var today = now.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            var donations = await _context.Donations
                .Include(d => d.Cause)
                .AsNoTracking()
                .ToListAsync();

            var completed = donations.Where(d => d.PaymentStatus == "Completed").ToList();

            var totalAmount = completed.Sum(d => d.Amount);
            var todayAmount = completed.Where(d => d.DonationDate >= today).Sum(d => d.Amount);
            var weekAmount = completed.Where(d => d.DonationDate >= startOfWeek).Sum(d => d.Amount);
            var monthAmount = completed.Where(d => d.DonationDate >= startOfMonth).Sum(d => d.Amount);
            var yearAmount = completed.Where(d => d.DonationDate >= startOfYear).Sum(d => d.Amount);

            var gatewayDistribution = completed
                .GroupBy(d => string.IsNullOrWhiteSpace(d.PaymentGateway) ? "Direct / Other" : d.PaymentGateway)
                .Select(g => new { Gateway = g.Key, Count = g.Count(), Total = g.Sum(x => x.Amount) })
                .ToList();

            var topCauses = completed
                .GroupBy(d => d.Cause?.Title ?? "General Charity")
                .Select(g => new { Cause = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderByDescending(x => x.Amount)
                .Take(5)
                .ToList();

            return Ok(new
            {
                totalFunds = totalAmount,
                totalTransactions = donations.Count,
                successfulCount = completed.Count,
                pendingCount = donations.Count(d => d.PaymentStatus == "Pending"),
                failedCount = donations.Count(d => d.PaymentStatus == "Failed"),
                todayFunds = todayAmount,
                weekFunds = weekAmount,
                monthFunds = monthAmount,
                yearFunds = yearAmount,
                gateways = gatewayDistribution,
                topCauses = topCauses,
                timestamp = now
            });
        }

        [HttpGet("analytics/campaigns")]
        public async Task<IActionResult> GetCampaignAnalytics()
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });

            var causes = await _context.Causes
                .Include(c => c.Donations)
                .AsNoTracking()
                .ToListAsync();

            var result = causes.Select(c => new
            {
                id = c.Id,
                title = c.Title,
                category = c.Category,
                targetAmount = c.TargetAmount,
                raisedAmount = c.RaisedAmount,
                progressPercentage = c.PercentageRaised,
                isActive = c.IsActive,
                isFeatured = c.IsFeatured,
                donationsCount = c.Donations.Count
            }).ToList();

            return Ok(new
            {
                totalCauses = causes.Count,
                activeCount = causes.Count(c => c.IsActive),
                featuredCount = causes.Count(c => c.IsFeatured),
                campaigns = result
            });
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetSystemHealth()
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });

            bool dbConnected = false;
            try
            {
                dbConnected = await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connectivity test failed in agent health check.");
            }

            return Ok(new
            {
                status = dbConnected ? "Healthy" : "Degraded",
                database = dbConnected ? "Connected" : "Disconnected",
                timestamp = DateTime.UtcNow
            });
        }

        [HttpPost("reports")]
        public async Task<IActionResult> RecordAgentReport([FromBody] AgentReport report)
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });
            if (report == null) return BadRequest("Invalid report payload.");

            report.Timestamp = DateTime.UtcNow;
            if (string.IsNullOrEmpty(report.ReportCode))
            {
                report.ReportCode = $"RPT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            }

            // Cryptographic tamper-resistant hash computation
            var rawData = $"{report.ReportCode}|{report.AgentName}|{report.ActionName}|{report.RiskLevel}|{report.Timestamp:O}";
            using var sha = SHA256.Create();
            report.IntegrityHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawData)));

            _context.AgentReports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, reportCode = report.ReportCode });
        }

        [HttpPost("security/events")]
        public async Task<IActionResult> RecordSecurityEvent([FromBody] AgentSecurityEvent secEvent)
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });
            if (secEvent == null) return BadRequest("Invalid security event payload.");

            secEvent.DetectedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(secEvent.EventCode))
            {
                secEvent.EventCode = $"SEC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            }

            _context.AgentSecurityEvents.Add(secEvent);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, eventCode = secEvent.EventCode });
        }

        public class RestrictionRequest
        {
            public string ViolationCategory { get; set; } = "PolicyViolation";
            public string Reason { get; set; } = "Platform policy violation detected.";
        }

        [HttpPost("users/{id}/restrict")]
        public async Task<IActionResult> RestrictUser(int id, [FromBody] RestrictionRequest req)
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "Target user not found." });

            // Permanent Super Admin protection
            if (string.Equals(user.Email, "admin@ngeos.org", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Permanent Super Administrator cannot be restricted." });
            }

            // Deactivate account
            user.IsActive = false;

            var restriction = new AgentUserRestriction
            {
                UserId = user.Id,
                ViolationCategory = req.ViolationCategory,
                Reason = req.Reason,
                RestrictedAt = DateTime.UtcNow,
                IsActive = true,
                CountdownCompleted = true
            };

            _context.AgentUserRestrictions.Add(restriction);

            // Log consequential audit report
            var report = new AgentReport
            {
                ReportCode = $"RPT-SUSP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                AgentName = "NGEOS Autonomous Security Engine",
                AgentType = "SecurityEngine",
                ActionName = "TemporaryAccountSuspension",
                ModuleName = "Security",
                TargetUserId = user.Id,
                ActionDescription = $"Applied temporary suspension to user {user.FullName} ({user.Email}).",
                ActionReason = req.Reason,
                PreviousValue = "Active=True",
                NewValue = "Active=False",
                RiskLevel = "High",
                ActionStatus = "Success",
                Timestamp = DateTime.UtcNow
            };
            _context.AgentReports.Add(report);

            await _context.SaveChangesAsync();

            // Send official suspension email asynchronously
            try
            {
                var subject = "Important: Your NGEOS Account Has Been Temporarily Suspended";
                var body = $@"<h3>NGEOS Platform Security Notice</h3>
<p>Dear {user.FullName},</p>
<p>Your account has been temporarily suspended due to a verified policy violation:</p>
<ul>
    <li><strong>Violation Category:</strong> {req.ViolationCategory}</li>
    <li><strong>Suspension Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
    <li><strong>Reason:</strong> {req.Reason}</li>
</ul>
<p>If you believe this was an error, you may submit an appeal through the NGEOS portal or contact official support.</p>
<br/>
<p>NGEOS Platform Governance Team</p>";

                await _emailSender.SendEmailAsync(user.Email, subject, body);
                restriction.SuspensionEmailSent = true;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch suspension notification email to {Email}", user.Email);
            }

            return Ok(new { success = true, restrictionId = restriction.Id });
        }

        public class BroadcastRequest
        {
            public string TargetGroup { get; set; } = "Admins";
            public string EventName { get; set; } = "AgentNotification";
            public object? Payload { get; set; }
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> BroadcastSignalR([FromBody] BroadcastRequest req)
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });

            try
            {
                if (req.TargetGroup.StartsWith("User_", StringComparison.OrdinalIgnoreCase))
                {
                    await _hubContext.Clients.Group(req.TargetGroup).SendAsync(req.EventName, req.Payload);
                }
                else if (string.Equals(req.TargetGroup, "Admins", StringComparison.OrdinalIgnoreCase))
                {
                    await _hubContext.Clients.Group("Admins").SendAsync(req.EventName, req.Payload);
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync(req.EventName, req.Payload);
                }
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting SignalR notification.");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
