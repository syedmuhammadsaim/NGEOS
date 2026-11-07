using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NGEOS.Data;

namespace NGEOS.Services.Ai.Grounding
{
    /// <summary>
    /// Executes predefined, read-only EF Core queries against NGEOSDb to extract
    /// live ground truth for the AI assistant without exposing PII.
    /// Covers all sections of the NGEOS platform: Causes, Donations, Zakat, Programmes,
    /// Events, NGOs, Partners, Volunteer Jobs, FAQs, Contact, and Navigation.
    /// </summary>
    public class NgeosKnowledgeService : INgeosKnowledgeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NgeosKnowledgeService> _logger;

        public NgeosKnowledgeService(ApplicationDbContext context, ILogger<NgeosKnowledgeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GetGroundingContextAsync(string userMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                var sb = new StringBuilder();
                var msg = (userMessage ?? string.Empty).ToLowerInvariant();

                sb.AppendLine("=== [NGEOS VERIFIED LIVE DATABASE CONTEXT] ===");
                sb.AppendLine($"System Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                sb.AppendLine("Platform: NGEOS (Next Generation NGO Management & Donation Platform / Give-AID)");
                sb.AppendLine("Headquarters: 100 Edhi Street, Saddar, Karachi, Pakistan | Phone: +92 21 3456 7890 | Email: support@ngeos.org");

                // 1. Core Platform Verified Totals (Always included)
                var completedDonationsQuery = _context.Donations.AsNoTracking().Where(d => d.PaymentStatus == "Completed");
                var totalCompletedFunds = await completedDonationsQuery.SumAsync(d => (decimal?)d.Amount, cancellationToken) ?? 0m;
                var totalCompletedCount = await completedDonationsQuery.CountAsync(cancellationToken);
                var activeCausesCount = await _context.Causes.AsNoTracking().CountAsync(c => c.IsActive, cancellationToken);
                var activeProgrammesCount = await _context.Programmes.AsNoTracking().CountAsync(p => p.IsActive, cancellationToken);
                var activeEventsCount = await _context.Events.AsNoTracking().CountAsync(e => e.IsUpcoming, cancellationToken);
                var activeJobCount = await _context.JobOpportunities.AsNoTracking().CountAsync(j => j.IsActive, cancellationToken);
                var verifiedNgosCount = await _context.NGOs.AsNoTracking().CountAsync(n => n.IsActive && n.IsVerified, cancellationToken);
                var activePartnersCount = await _context.Partners.AsNoTracking().CountAsync(p => p.IsActive, cancellationToken);

                sb.AppendLine("\n[OFFICIAL FINANCIAL & PLATFORM METRICS]");
                sb.AppendLine($"- Total Verified Funds Mobilized: Rs {totalCompletedFunds:N2} PKR (from {totalCompletedCount} confirmed completed transactions)");
                sb.AppendLine($"- Active Welfare Causes: {activeCausesCount}");
                sb.AppendLine($"- Active Community Programmes: {activeProgrammesCount}");
                sb.AppendLine($"- Upcoming Charity Events: {activeEventsCount}");
                sb.AppendLine($"- Verified Partner NGOs: {verifiedNgosCount}");
                sb.AppendLine($"- Active Corporate & Tech Partners: {activePartnersCount}");
                sb.AppendLine($"- Active Job & Volunteer Openings: {activeJobCount}");
                sb.AppendLine("- Payment Rule: ONLY completed transactions are counted. Pending, cancelled, or failed transactions are strictly excluded.");

                // Navigation Sitemap
                sb.AppendLine("\n[OFFICIAL WEBSITE SITEMAP & ROUTES]");
                sb.AppendLine("- Home: /");
                sb.AppendLine("- Welfare Causes: /Causes (Browse all campaigns and donate)");
                sb.AppendLine("- Community Programmes: /Programmes (Educational, healthcare, and empowerment drives)");
                sb.AppendLine("- Charity Events: /Events (Marathons, charity galas, blood donation expos)");
                sb.AppendLine("- Smart Zakat Calculator: /Zakat (Instant Islamic Zakat computation)");
                sb.AppendLine("- Associated NGOs Directory: /Ngos (List of verified NGO partners)");
                sb.AppendLine("- Career & Volunteer Hub: /About/Career (Open volunteer and job positions)");
                sb.AppendLine("- About Us Overview: /About (Mission, leadership team, achievements)");
                sb.AppendLine("- Support Query Desk: /Home/RaiseQuery (Submit an official inquiry)");
                sb.AppendLine("- Contact Us: /Home/Contact");

                // 2. Specific intent-based data retrieval
                bool askCauses = msg.Contains("cause") || msg.Contains("campaign") || msg.Contains("need") || 
                                 msg.Contains("orphan") || msg.Contains("school") || msg.Contains("health") || 
                                 msg.Contains("target") || msg.Contains("donate") || msg.Contains("welfare") ||
                                 msg.Contains("fund") || msg.Contains("list");

                bool askDonations = msg.Contains("donation") || msg.Contains("zakat") || msg.Contains("sadqah") || 
                                    msg.Contains("fitrah") || msg.Contains("money") || msg.Contains("ledger") || 
                                    msg.Contains("amount") || msg.Contains("received") || msg.Contains("total") ||
                                    msg.Contains("paisa") || msg.Contains("paise") || msg.Contains("card") ||
                                    msg.Contains("bank") || msg.Contains("nisab");

                bool askProgrammes = msg.Contains("programme") || msg.Contains("program") || msg.Contains("caravan") || 
                                     msg.Contains("workshop") || msg.Contains("sport") || msg.Contains("accelerator");

                bool askEvents = msg.Contains("event") || msg.Contains("gala") || msg.Contains("walk") || 
                                 msg.Contains("marathon") || msg.Contains("expo") || msg.Contains("blood");

                bool askVolunteers = msg.Contains("volunteer") || msg.Contains("job") || msg.Contains("career") || 
                                     msg.Contains("hiring") || msg.Contains("internship") || msg.Contains("apply") || 
                                     msg.Contains("join") || msg.Contains("work") || msg.Contains("kaam");

                bool askNgos = msg.Contains("ngo") || msg.Contains("partner") || msg.Contains("alliance") || 
                               msg.Contains("organization") || msg.Contains("trust") || msg.Contains("sponsor");

                bool askFaq = msg.Contains("how to") || msg.Contains("faq") || msg.Contains("tax") || 
                              msg.Contains("receipt") || msg.Contains("refund") || msg.Contains("help") || 
                              msg.Contains("contact") || msg.Contains("kaise") || msg.Contains("kahan") ||
                              msg.Contains("number") || msg.Contains("email") || msg.Contains("address");

                bool askAbout = msg.Contains("about") || msg.Contains("mission") || msg.Contains("vision") || 
                                msg.Contains("who") || msg.Contains("what is") || msg.Contains("kya hai") ||
                                msg.Contains("team") || msg.Contains("leader");

                // Default fallback if query is general greeting or broad inquiry
                if (!askCauses && !askDonations && !askProgrammes && !askEvents && !askVolunteers && !askNgos && !askFaq && !askAbout)
                {
                    askCauses = true;
                    askDonations = true;
                    askProgrammes = true;
                }

                // A. Active Causes
                if (askCauses || askDonations)
                {
                    var causes = await _context.Causes.AsNoTracking()
                        .Where(c => c.IsActive)
                        .OrderByDescending(c => c.IsFeatured)
                        .ThenByDescending(c => c.RaisedAmount)
                        .Select(c => new
                        {
                            c.Id,
                            c.Title,
                            c.Category,
                            c.TargetAmount,
                            c.RaisedAmount,
                            c.IsFeatured,
                            c.ShortDescription
                        })
                        .Take(7)
                        .ToListAsync(cancellationToken);

                    sb.AppendLine("\n[ACTIVE WELFARE CAUSES]");
                    foreach (var cause in causes)
                    {
                        var pct = cause.TargetAmount > 0 ? Math.Round((cause.RaisedAmount / cause.TargetAmount) * 100, 1) : 0;
                        sb.AppendLine($"- Cause #{cause.Id}: \"{cause.Title}\" | Category: {cause.Category} | Target: ${cause.TargetAmount:N2} | Raised: ${cause.RaisedAmount:N2} ({pct}%) | Featured: {cause.IsFeatured}");
                        sb.AppendLine($"  Description: {cause.ShortDescription}");
                    }
                }

                // B. Verified Donation Breakdown (Zakat vs General) & Payment Guide
                if (askDonations)
                {
                    var zakatDonations = await _context.Donations.AsNoTracking()
                        .Where(d => d.PaymentStatus == "Completed" && d.Notes != null && d.Notes.Contains("[Type:Zakat]"))
                        .SumAsync(d => (decimal?)d.Amount, cancellationToken) ?? 0m;

                    var generalDonations = totalCompletedFunds - zakatDonations;

                    sb.AppendLine("\n[VERIFIED DONATION FUND ALLOCATION]");
                    sb.AppendLine($"- Verified Zakat Collected: Rs {zakatDonations:N2} PKR");
                    sb.AppendLine($"- Verified General Donations: Rs {generalDonations:N2} PKR");
                    sb.AppendLine("- Supported Modes: Online Credit/Debit Card (Visa, MasterCard), Bank Transfer");
                    sb.AppendLine("- Donation Types: General Fund, Zakat, Sadqah, Fitrah");
                    sb.AppendLine("\n[HOW TO DONATE STEP-BY-STEP]");
                    sb.AppendLine("1. Visit /Causes and select a campaign.");
                    sb.AppendLine("2. Click the 'Donate Now' button.");
                    sb.AppendLine("3. Select a preset amount ($25, $50, $100, $250) or type a custom amount.");
                    sb.AppendLine("4. Choose donation type: General, Zakat, Sadqah, or Fitrah.");
                    sb.AppendLine("5. Enter payment details securely and confirm.");
                    sb.AppendLine("6. Receive an instant digital receipt with a verified Transaction ID.");

                    sb.AppendLine("\n[SMART ZAKAT CALCULATOR GUIDELINES (/Zakat)]");
                    sb.AppendLine("- Zakat Rate: 2.5% on qualifying wealth held for one full lunar year above the Nisab threshold.");
                    sb.AppendLine("- Nisab Threshold: 7.5 Tola Gold (~87.48 grams) or 52.5 Tola Silver (~612.36 grams).");
                    sb.AppendLine("- Deduct immediate liabilities/debts from liquid assets before calculating 2.5%.");
                }

                // C. Active Community Programmes
                if (askProgrammes)
                {
                    var programmes = await _context.Programmes.AsNoTracking()
                        .Where(p => p.IsActive)
                        .OrderBy(p => p.StartDate)
                        .Select(p => new
                        {
                            p.Id,
                            p.Title,
                            p.Category,
                            p.StartDate,
                            p.Location,
                            p.TargetBeneficiaries,
                            p.ShortDescription
                        })
                        .Take(5)
                        .ToListAsync(cancellationToken);

                    sb.AppendLine("\n[ACTIVE COMMUNITY PROGRAMMES (/Programmes)]");
                    foreach (var prog in programmes)
                    {
                        sb.AppendLine($"- Programme #{prog.Id}: \"{prog.Title}\" | Category: {prog.Category} | Starts: {prog.StartDate:MMM dd, yyyy} | Location: {prog.Location} | Target Beneficiaries: {prog.TargetBeneficiaries}+");
                        sb.AppendLine($"  Summary: {prog.ShortDescription}");
                    }
                }

                // D. Upcoming Charity Events
                if (askEvents)
                {
                    var events = await _context.Events.AsNoTracking()
                        .Where(e => e.IsUpcoming)
                        .OrderBy(e => e.EventDate)
                        .Select(e => new
                        {
                            e.Id,
                            e.Title,
                            e.EventDate,
                            e.Location,
                            e.Organizer,
                            e.ShortDescription
                        })
                        .Take(5)
                        .ToListAsync(cancellationToken);

                    sb.AppendLine("\n[UPCOMING CHARITY EVENTS (/Events)]");
                    foreach (var ev in events)
                    {
                        sb.AppendLine($"- Event #{ev.Id}: \"{ev.Title}\" | Date: {ev.EventDate:MMM dd, yyyy} | Location: {ev.Location} | Organizer: {ev.Organizer}");
                        sb.AppendLine($"  Overview: {ev.ShortDescription}");
                    }
                }

                // E. Job and Volunteer Opportunities
                if (askVolunteers)
                {
                    var jobs = await _context.JobOpportunities.AsNoTracking()
                        .Where(j => j.IsActive)
                        .OrderByDescending(j => j.PostedDate)
                        .Select(j => new
                        {
                            j.Id,
                            j.Title,
                            j.Department,
                            j.JobType,
                            j.Location,
                            j.Description
                        })
                        .Take(5)
                        .ToListAsync(cancellationToken);

                    sb.AppendLine("\n[VOLUNTEER & CAREER OPENINGS (/About/Career)]");
                    foreach (var job in jobs)
                    {
                        sb.AppendLine($"- Opportunity #{job.Id}: \"{job.Title}\" ({job.JobType}) | Dept: {job.Department} | Location: {job.Location}");
                        sb.AppendLine($"  Role Overview: {job.Description}");
                    }
                    sb.AppendLine("- How to Apply: Visit /About/Career and submit your application online.");
                }

                // F. Verified Partner NGOs & Corporate Partners
                if (askNgos)
                {
                    var ngos = await _context.NGOs.AsNoTracking()
                        .Where(n => n.IsActive && n.IsVerified)
                        .Select(n => new
                        {
                            n.Id,
                            n.Name,
                            n.RegistrationNumber,
                            n.FocusArea,
                            n.EstablishedYear
                        })
                        .Take(6)
                        .ToListAsync(cancellationToken);

                    sb.AppendLine("\n[VERIFIED PARTNER NGOS (/Ngos)]");
                    foreach (var ngo in ngos)
                    {
                        sb.AppendLine($"- NGO: {ngo.Name} | Reg #: {ngo.RegistrationNumber} | Focus: {ngo.FocusArea} | Est. {ngo.EstablishedYear}");
                    }

                    var partners = await _context.Partners.AsNoTracking()
                        .Where(p => p.IsActive)
                        .Select(p => new { p.Name, p.Category, p.Description })
                        .Take(5)
                        .ToListAsync(cancellationToken);

                    sb.AppendLine("\n[CORPORATE & TECH PARTNERS]");
                    foreach (var partner in partners)
                    {
                        sb.AppendLine($"- Partner: {partner.Name} | Type: {partner.Category} | Info: {partner.Description}");
                    }
                }

                // G. Published FAQs
                if (askFaq)
                {
                    var faqs = await _context.FAQs.AsNoTracking()
                        .Where(f => f.IsActive)
                        .OrderBy(f => f.DisplayOrder)
                        .Select(f => new
                        {
                            f.Question,
                            f.Answer,
                            f.Category
                        })
                        .Take(6)
                        .ToListAsync(cancellationToken);

                    sb.AppendLine("\n[OFFICIAL FREQUENTLY ASKED QUESTIONS]");
                    foreach (var faq in faqs)
                    {
                        sb.AppendLine($"Q: {faq.Question} ({faq.Category})");
                        sb.AppendLine($"A: {faq.Answer}");
                    }
                }

                // H. Official Mission, What We Do, & Team
                if (askAbout)
                {
                    var pages = await _context.AboutPages.AsNoTracking()
                        .Where(a => a.Slug == "what-we-do" || a.Slug == "our-mission" || a.Slug == "our-achievements")
                        .Select(a => new { a.Slug, a.Title, a.Subtitle })
                        .ToListAsync(cancellationToken);

                    sb.AppendLine("\n[ORGANIZATION OVERVIEW (/About)]");
                    sb.AppendLine("NGEOS is an integrated, next-generation welfare platform uniting grassroots NGOs, donors, and volunteers for transparent philanthropy across Pakistan and globally.");
                    foreach (var p in pages)
                    {
                        sb.AppendLine($"- {p.Title}: {p.Subtitle}");
                    }
                }

                sb.AppendLine("=== [END VERIFIED CONTEXT] ===");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching NGEOS knowledge grounding context.");
                return "=== [NGEOS LIVE CONTEXT TEMPORARILY UNAVAILABLE] ===";
            }
        }
    }
}
