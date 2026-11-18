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

        [HttpGet("sections/summary")]
        public async Task<IActionResult> GetEcosystemSectionsSummary()
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });

            var now = DateTime.UtcNow;

            // 1. Fundraising & Ledger
            var causes = await _context.Causes.AsNoTracking().ToListAsync();
            var donations = await _context.Donations.AsNoTracking().ToListAsync();
            var completedDonations = donations.Where(d => d.PaymentStatus == "Completed").ToList();

            // 2. Members
            var users = await _context.Users.AsNoTracking().ToListAsync();

            // 3. Organization
            var ngos = await _context.NGOs.AsNoTracking().ToListAsync();
            var partners = await _context.Partners.AsNoTracking().ToListAsync();
            var programmes = await _context.Programmes.AsNoTracking().ToListAsync();
            var volunteerInterests = await _context.ProgrammeInterests.AsNoTracking().ToListAsync();
            var events = await _context.Events.AsNoTracking().ToListAsync();

            // 4. Content (CMS)
            var galleryCount = await _context.GalleryItems.CountAsync();
            var aboutPagesCount = await _context.AboutPages.CountAsync();
            var teamCount = await _context.TeamMembers.CountAsync();
            var jobs = await _context.JobOpportunities.AsNoTracking().ToListAsync();
            var faqsCount = await _context.FAQs.CountAsync();

            // 5. Support
            var queries = await _context.UserQueries.AsNoTracking().ToListAsync();
            var contactMessages = await _context.ContactMessages.AsNoTracking().ToListAsync();

            // 6. Security & AI Governance
            var securityEventsCount = await _context.AgentSecurityEvents.CountAsync();
            var restrictionsCount = await _context.AgentUserRestrictions.CountAsync(r => r.IsActive);
            var pendingAppealsCount = await _context.AgentAppeals.CountAsync(a => a.Status == "Pending");

            return Ok(new
            {
                timestamp = now,
                causes = new
                {
                    total = causes.Count,
                    active = causes.Count(c => c.IsActive),
                    featured = causes.Count(c => c.IsFeatured),
                    totalTarget = causes.Sum(c => c.TargetAmount),
                    totalRaised = causes.Sum(c => c.RaisedAmount)
                },
                donations = new
                {
                    totalCount = donations.Count,
                    completedCount = completedDonations.Count,
                    pendingCount = donations.Count(d => d.PaymentStatus == "Pending"),
                    failedCount = donations.Count(d => d.PaymentStatus == "Failed"),
                    totalAmount = completedDonations.Sum(d => d.Amount)
                },
                members = new
                {
                    total = users.Count,
                    active = users.Count(u => u.IsActive),
                    inactive = users.Count(u => !u.IsActive),
                    admins = users.Count(u => u.Role == "Admin"),
                    superAdminProtected = true
                },
                ngos = new
                {
                    total = ngos.Count,
                    verified = ngos.Count(n => n.IsVerified),
                    unverified = ngos.Count(n => !n.IsVerified)
                },
                partners = new
                {
                    total = partners.Count,
                    active = partners.Count(p => p.IsActive)
                },
                programmes = new
                {
                    total = programmes.Count,
                    active = programmes.Count(p => p.IsActive),
                    upcoming = programmes.Count(p => p.StartDate > now)
                },
                volunteers = new
                {
                    total = volunteerInterests.Count,
                    pending = volunteerInterests.Count(v => v.Status == "Pending"),
                    approved = volunteerInterests.Count(v => v.Status == "Approved")
                },
                events = new
                {
                    total = events.Count,
                    upcoming = events.Count(e => e.IsUpcoming || e.EventDate >= now),
                    past = events.Count(e => !e.IsUpcoming && e.EventDate < now)
                },
                cms = new
                {
                    galleryCount = galleryCount,
                    aboutPagesCount = aboutPagesCount,
                    teamCount = teamCount,
                    totalJobs = jobs.Count,
                    activeJobs = jobs.Count(j => j.IsActive),
                    faqsCount = faqsCount
                },
                support = new
                {
                    totalQueries = queries.Count,
                    pendingQueries = queries.Count(q => q.Status == "Pending"),
                    resolvedQueries = queries.Count(q => q.Status == "Replied" || q.Status == "Closed"),
                    totalContactMessages = contactMessages.Count,
                    unreadContactMessages = contactMessages.Count(m => !m.IsRead)
                },
                security = new
                {
                    totalEvents = securityEventsCount,
                    activeRestrictions = restrictionsCount,
                    pendingAppeals = pendingAppealsCount
                }
            });
        }

        [HttpGet("support/queries/pending")]
        public async Task<IActionResult> GetPendingQueries()
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });

            var pending = await _context.UserQueries
                .Where(q => q.Status == "Pending")
                .OrderBy(q => q.CreatedAt)
                .Take(25)
                .Select(q => new
                {
                    id = q.Id,
                    fullName = q.FullName,
                    email = q.Email,
                    category = q.Category,
                    subject = q.Subject,
                    message = q.Message,
                    createdAt = q.CreatedAt,
                    hoursOpen = Math.Round((DateTime.UtcNow - q.CreatedAt).TotalHours, 1)
                })
                .ToListAsync();

            return Ok(pending);
        }

        [HttpGet("support/messages/unread")]
        public async Task<IActionResult> GetUnreadContactMessages()
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });

            var unread = await _context.ContactMessages
                .Where(m => !m.IsRead)
                .OrderBy(m => m.CreatedAt)
                .Take(25)
                .Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    email = m.Email,
                    subject = m.Subject,
                    message = m.Message,
                    createdAt = m.CreatedAt,
                    hoursOpen = Math.Round((DateTime.UtcNow - m.CreatedAt).TotalHours, 1)
                })
                .ToListAsync();

            return Ok(unread);
        }

        [HttpGet("organization/volunteers/pending")]
        public async Task<IActionResult> GetPendingVolunteers()
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });

            var pending = await _context.ProgrammeInterests
                .Include(p => p.Programme)
                .Where(v => v.Status == "Pending")
                .OrderBy(v => v.SubmittedAt)
                .Take(25)
                .Select(v => new
                {
                    id = v.Id,
                    fullName = v.FullName,
                    email = v.Email,
                    programmeTitle = v.Programme != null ? v.Programme.Title : "General Welfare",
                    interestType = v.InterestType,
                    message = v.Message,
                    submittedAt = v.SubmittedAt
                })
                .ToListAsync();

            return Ok(pending);
        }

        public class ActionExecutionRequest
        {
            public string ActionName { get; set; } = string.Empty;
            public string ModuleName { get; set; } = string.Empty;
            public int? TargetEntityId { get; set; }
            public string? ParametersJson { get; set; }
            public string Reason { get; set; } = "Automated AI Ecosystem Operation";
        }

        [HttpPost("actions/execute")]
        public async Task<IActionResult> ExecutePermittedAction([FromBody] ActionExecutionRequest req)
        {
            if (!ValidateInternalKey()) return Unauthorized(new { message = "Invalid internal service key." });
            if (string.IsNullOrWhiteSpace(req.ActionName)) return BadRequest(new { message = "Action name is required." });

            var report = new AgentReport
            {
                ReportCode = $"RPT-ACT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                AgentName = "NGEOS Autonomous Admin Agent",
                AgentType = "AdminAgent",
                ModuleName = req.ModuleName,
                ActionName = req.ActionName,
                ActionReason = req.Reason,
                RiskLevel = "Low",
                Timestamp = DateTime.UtcNow
            };

            try
            {
                switch (req.ActionName)
                {
                    case "RecalculateCampaignProgress":
                        var causes = await _context.Causes.Include(c => c.Donations).ToListAsync();
                        int updatedCount = 0;
                        foreach (var c in causes)
                        {
                            var totalCompleted = c.Donations.Where(d => d.PaymentStatus == "Completed").Sum(d => d.Amount);
                            c.RaisedAmount = totalCompleted;
                            updatedCount++;
                        }
                        await _context.SaveChangesAsync();
                        report.ActionDescription = $"Recalculated financial progress across {updatedCount} welfare causes.";
                        report.ActionStatus = "Success";
                        break;

                    case "FlagExpiredJobs":
                        var threshold = DateTime.UtcNow.AddDays(-60);
                        var expiredJobs = await _context.JobOpportunities.Where(j => j.IsActive && j.PostedDate < threshold).ToListAsync();
                        foreach (var j in expiredJobs)
                        {
                            j.IsActive = false;
                        }
                        await _context.SaveChangesAsync();
                        report.ActionDescription = $"Archived {expiredJobs.Count} stale job vacancies older than 60 days.";
                        report.ActionStatus = "Success";
                        break;

                    case "MarkContactMessageRead":
                        if (req.TargetEntityId.HasValue)
                        {
                            var msg = await _context.ContactMessages.FindAsync(req.TargetEntityId.Value);
                            if (msg != null)
                            {
                                msg.IsRead = true;
                                msg.ReplyNotes = $"[Agent Triaged]: Reviewed for validity on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.";
                                await _context.SaveChangesAsync();
                                report.ActionDescription = $"Marked contact message #{msg.Id} from {msg.Email} as read/triaged.";
                                report.ActionStatus = "Success";
                            }
                        }
                        break;

                    case "TriageVolunteerApplication":
                        if (req.TargetEntityId.HasValue)
                        {
                            var interest = await _context.ProgrammeInterests.FindAsync(req.TargetEntityId.Value);
                            if (interest != null && interest.Status == "Pending")
                            {
                                interest.Status = "Contacted";
                                await _context.SaveChangesAsync();
                                report.ActionDescription = $"Triaged volunteer #{interest.Id} ({interest.FullName}) to 'Contacted' status.";
                                report.ActionStatus = "Success";
                            }
                        }
                        break;

                    default:
                        return BadRequest(new { message = $"Unrecognized or non-permitted autonomous action '{req.ActionName}'." });
                }

                // Compute cryptographic tamper-resistant hash
                var rawData = $"{report.ReportCode}|{report.AgentName}|{report.ActionName}|{report.RiskLevel}|{report.Timestamp:O}";
                using var sha = SHA256.Create();
                report.IntegrityHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawData)));

                _context.AgentReports.Add(report);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    reportCode = report.ReportCode,
                    action = req.ActionName,
                    description = report.ActionDescription
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute permitted agent action '{ActionName}'", req.ActionName);
                report.ActionStatus = "Failed";
                report.ActionDescription = $"Execution error: {ex.Message}";
                report.IntegrityHash = "ERROR";
                _context.AgentReports.Add(report);
                await _context.SaveChangesAsync();

                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
