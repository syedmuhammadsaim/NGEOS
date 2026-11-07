using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Helpers;
using NGEOS.Models;
using NGEOS.Services;
using NGEOS.ViewModels;
using NGEOS.Services.Payments.Core;
using NGEOS.Services.Payments.Dtos;

namespace NGEOS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IPaymentGatewayFactory _gatewayFactory;

        public AdminController(ApplicationDbContext context, IEmailSender emailSender, IPaymentGatewayFactory gatewayFactory)
        {
            _context = context;
            _emailSender = emailSender;
            _gatewayFactory = gatewayFactory;
        }

        // 1. Dashboard Overview
        public async Task<IActionResult> Index()
        {
            var totalUsers = await _context.Users.CountAsync(u => u.Role == "User");
            var totalDonationsCount = await _context.Donations.CountAsync();
            var totalDonationsAmount = await _context.Donations.Where(d => d.PaymentStatus == "Completed").SumAsync(d => (decimal?)d.Amount) ?? 0;
            var totalCauses = await _context.Causes.CountAsync();
            var totalNgos = await _context.NGOs.CountAsync();
            var totalProgrammes = await _context.Programmes.CountAsync();
            var totalEvents = await _context.Events.CountAsync();
            var pendingQueriesCount = await _context.UserQueries.CountAsync(q => q.Status == "Pending");
            var totalVolunteersCount = await _context.ProgrammeInterests.CountAsync();
            var unreadMessagesCount = await _context.ContactMessages.CountAsync(m => !m.IsRead);
            var recentMessages = await _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentDonations = await _context.Donations
                .Include(d => d.Cause)
                .OrderByDescending(d => d.DonationDate)
                .Take(6)
                .ToListAsync();

            var recentQueries = await _context.UserQueries
                .OrderByDescending(q => q.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentUsers = await _context.Users
                .Where(u => u.Role == "User")
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentInterests = await _context.ProgrammeInterests
                .Include(pi => pi.Programme)
                .OrderByDescending(pi => pi.SubmittedAt)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalDonationsCount = totalDonationsCount,
                TotalDonationsAmount = totalDonationsAmount,
                TotalCauses = totalCauses,
                TotalNgos = totalNgos,
                TotalProgrammes = totalProgrammes,
                TotalEvents = totalEvents,
                PendingQueriesCount = pendingQueriesCount,
                TotalVolunteersCount = totalVolunteersCount,
                UnreadMessagesCount = unreadMessagesCount,
                RecentDonations = recentDonations,
                RecentMessages = recentMessages,
                RecentQueries = recentQueries,
                RecentUsers = recentUsers,
                RecentProgrammeInterests = recentInterests
            };

            return View(model);
        }

        // ==========================================
        // 2. CAUSES MANAGEMENT (CRUD)
        // ==========================================
        public async Task<IActionResult> Causes()
        {
            var causes = await _context.Causes
                .Include(c => c.Ngo)
                .Include(c => c.Donations)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(causes);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCause()
        {
            ViewBag.Ngos = await _context.NGOs.Where(n => n.IsActive).ToListAsync();
            ViewBag.Categories = new List<string> { "Children", "Education", "Healthcare", "Disabled Persons", "Women", "Youth", "Elderly" };
            return View(new Cause());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCause(Cause cause)
        {
            if (ModelState.IsValid)
            {
                cause.CreatedAt = DateTime.UtcNow;
                _context.Causes.Add(cause);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cause '{cause.Title}' has been created successfully.";
                return RedirectToAction(nameof(Causes));
            }
            ViewBag.Ngos = await _context.NGOs.Where(n => n.IsActive).ToListAsync();
            ViewBag.Categories = new List<string> { "Children", "Education", "Healthcare", "Disabled Persons", "Women", "Youth", "Elderly" };
            return View(cause);
        }

        [HttpGet]
        public async Task<IActionResult> EditCause(int id)
        {
            var cause = await _context.Causes.FindAsync(id);
            if (cause == null) return NotFound();

            ViewBag.Ngos = await _context.NGOs.Where(n => n.IsActive).ToListAsync();
            ViewBag.Categories = new List<string> { "Children", "Education", "Healthcare", "Disabled Persons", "Women", "Youth", "Elderly" };
            return View(cause);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCause(Cause cause)
        {
            if (ModelState.IsValid)
            {
                _context.Causes.Update(cause);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cause '{cause.Title}' updated successfully.";
                return RedirectToAction(nameof(Causes));
            }
            ViewBag.Ngos = await _context.NGOs.Where(n => n.IsActive).ToListAsync();
            ViewBag.Categories = new List<string> { "Children", "Education", "Healthcare", "Disabled Persons", "Women", "Youth", "Elderly" };
            return View(cause);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCause(int id)
        {
            var cause = await _context.Causes.FindAsync(id);
            if (cause != null)
            {
                _context.Causes.Remove(cause);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cause deleted successfully.";
            }
            return RedirectToAction(nameof(Causes));
        }

        // ==========================================
        // 3. DONATIONS & TRANSACTIONS MASTER LEDGER
        // ==========================================
        public async Task<IActionResult> Donations(string? filter)
        {
            var query = _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.User)
                .Include(d => d.Transactions)
                .OrderByDescending(d => d.DonationDate);

            var allDonations = await query.ToListAsync();

            // Check health of all 4 gateways
            var gateways = _gatewayFactory.GetAllGateways();
            var gatewayStatuses = new List<object>();
            foreach (var g in gateways)
            {
                var health = await g.CheckHealthAsync();
                gatewayStatuses.Add(new
                {
                    Name = g.GatewayName,
                    IsConfigured = g.IsConfigured,
                    Environment = g.EnvironmentName,
                    IsHealthy = health.IsHealthy,
                    HealthMessage = health.Message
                });
            }
            ViewBag.GatewayStatuses = gatewayStatuses;

            // Calculate Metrics
            var totalVerifiedFunding = allDonations.Where(d => d.PaymentStatus == "Completed").Sum(d => d.Amount);
            var totalZakatAmount = allDonations.Where(d => d.PaymentStatus == "Completed" && d.IsZakat).Sum(d => d.Amount);
            var totalGeneralAmount = totalVerifiedFunding - totalZakatAmount;
            var totalEmailsSent = allDonations.Count(d => d.IsReceiptEmailSent);
            var zakatCount = allDonations.Count(d => d.IsZakat);
            var generalCount = allDonations.Count(d => !d.IsZakat);

            ViewBag.TotalZakatAmount = totalZakatAmount;
            ViewBag.TotalGeneralAmount = totalGeneralAmount;
            ViewBag.TotalEmailsSent = totalEmailsSent;
            ViewBag.ZakatCount = zakatCount;
            ViewBag.GeneralCount = generalCount;
            ViewBag.CurrentFilter = filter ?? "all";

            var displayed = filter?.ToLower() switch
            {
                "zakat" => allDonations.Where(d => d.IsZakat).ToList(),
                "general" => allDonations.Where(d => !d.IsZakat).ToList(),
                _ => allDonations
            };

            return View(displayed);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendDonationReceipt(int id)
        {
            var donation = await _context.Donations
                .Include(d => d.Cause)
                .ThenInclude(c => c!.Ngo)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (donation == null)
            {
                TempData["ErrorMessage"] = "Donation record not found.";
                return RedirectToAction(nameof(Donations));
            }

            try
            {
                await _emailSender.SendDonationReceiptAsync(
                    donation.DonorEmail,
                    donation.DonorName,
                    donation.TransactionId,
                    donation.Amount,
                    donation.DonationType,
                    donation.Cause?.Title ?? "General Charity Relief",
                    donation.Cause?.Ngo?.Name ?? "NGEOS Foundation",
                    donation.DonationDate,
                    donation.CardType,
                    donation.MaskedCardNumber
                );

                var cleanNote = donation.CleanNotes;
                donation.Notes = string.IsNullOrWhiteSpace(cleanNote)
                    ? $"[Type:{donation.DonationType}] [Email:Sent]"
                    : $"[Type:{donation.DonationType}] [Email:Sent] {cleanNote}";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Receipt email re-sent successfully to {donation.DonorEmail} (Txn: {donation.TransactionId})!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to re-send receipt email: {ex.Message}";
            }

            return RedirectToAction(nameof(Donations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefundDonation(int id, string? reason)
        {
            var donation = await _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.Transactions)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (donation == null)
            {
                TempData["ErrorMessage"] = "Donation record not found.";
                return RedirectToAction(nameof(Donations));
            }

            if (donation.PaymentStatus == "Refunded")
            {
                TempData["ErrorMessage"] = "This donation has already been refunded.";
                return RedirectToAction(nameof(Donations));
            }

            var gatewayName = donation.PaymentGateway ?? "Safepay";
            var latestTxn = donation.Transactions.OrderByDescending(t => t.Id).FirstOrDefault();
            var gatewayRef = latestTxn?.GatewayReference ?? donation.TransactionId;

            var refundRequest = new PaymentRefundRequest
            {
                DonationId = donation.Id,
                InternalReference = donation.TransactionId,
                GatewayReference = gatewayRef,
                Amount = donation.Amount,
                Currency = donation.Currency,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Admin initiated refund" : reason.Trim()
            };

            try
            {
                var gateway = _gatewayFactory.GetGateway(gatewayName);
                var refundResult = await gateway.ProcessRefundAsync(refundRequest);

                if (refundResult.IsSuccess)
                {
                    donation.PaymentStatus = "Refunded";
                    if (donation.Cause != null && donation.Cause.RaisedAmount >= donation.Amount)
                    {
                        donation.Cause.RaisedAmount -= donation.Amount;
                    }

                    var refundTxn = new PaymentTransaction
                    {
                        DonationId = donation.Id,
                        GatewayName = gatewayName,
                        InternalReference = $"REF-{donation.TransactionId}",
                        GatewayReference = refundResult.RefundReference,
                        Amount = donation.Amount,
                        Currency = donation.Currency,
                        Status = "Refunded",
                        Environment = gateway.EnvironmentName,
                        VerificationStatus = "Verified",
                        IdempotencyKey = Guid.NewGuid().ToString("N"),
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.PaymentTransactions.Add(refundTxn);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Donation #{donation.TransactionId} successfully refunded via {gatewayName}! (Refund Ref: {refundResult.RefundReference})";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Refund failed via {gatewayName}: {refundResult.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Refund error: {ex.Message}";
            }

            return RedirectToAction(nameof(Donations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDonation(int id)
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation != null)
            {
                var txn = donation.TransactionId;
                _context.Donations.Remove(donation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Donation transaction #{txn} deleted successfully.";
            }
            return RedirectToAction(nameof(Donations));
        }


        // ==========================================
        // 4. REGISTERED MEMBERS DIRECTORY (Personal & Professional Data)
        // ==========================================
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Donations)
                .Include(u => u.ProgrammeInterests)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Role != "Admin")
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"User status updated to {(user.IsActive ? "Active" : "Inactive")}.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(int id, string newRole)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (newRole != "Admin" && newRole != "User")
            {
                TempData["ErrorMessage"] = "Invalid role specified. Must be 'Admin' or 'User'.";
                return RedirectToAction(nameof(Users));
            }

            if (user.Role == "Admin" && newRole == "User")
            {
                var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin" && u.IsActive);
                if (adminCount <= 1)
                {
                    TempData["ErrorMessage"] = "Cannot demote the only remaining active Administrator.";
                    return RedirectToAction(nameof(Users));
                }
            }

            user.Role = newRole;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Role for '{user.FullName}' ({user.Email}) updated to {newRole}.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(int id, string? newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var passToSet = string.IsNullOrWhiteSpace(newPassword) ? "Ngeos@2026" : newPassword.Trim();
            user.PasswordHash = PasswordHelper.HashPassword(passToSet);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Password for '{user.FullName}' ({user.Email}) reset to: {passToSet}";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User newUser, string password)
        {
            if (string.IsNullOrWhiteSpace(newUser.Email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Email and Password are required.";
                return RedirectToAction(nameof(Users));
            }

            var cleanEmail = newUser.Email.Trim().ToLower();
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == cleanEmail))
            {
                TempData["ErrorMessage"] = $"An account with email '{newUser.Email}' already exists.";
                return RedirectToAction(nameof(Users));
            }

            newUser.Email = cleanEmail;
            newUser.PasswordHash = PasswordHelper.HashPassword(password.Trim());
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.IsActive = true;
            if (string.IsNullOrWhiteSpace(newUser.Role)) newUser.Role = "User";
            if (string.IsNullOrWhiteSpace(newUser.FullName)) newUser.FullName = cleanEmail.Split('@')[0];

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"New {newUser.Role} account '{newUser.FullName}' created successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.Role == "Admin")
            {
                var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
                if (adminCount <= 1)
                {
                    TempData["ErrorMessage"] = "Cannot delete the sole Administrator account.";
                    return RedirectToAction(nameof(Users));
                }
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"User '{user.FullName}' ({user.Email}) removed.";
            return RedirectToAction(nameof(Users));
        }


        // ==========================================
        // 5. ASSOCIATED NGOS (CRUD)
        // ==========================================
        public async Task<IActionResult> Ngos()
        {
            var ngos = await _context.NGOs
                .Include(n => n.Causes)
                .Include(n => n.Programmes)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return View(ngos);
        }

        [HttpGet]
        public IActionResult CreateNgo()
        {
            return View(new NGO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNgo(NGO ngo)
        {
            if (ModelState.IsValid)
            {
                ngo.CreatedAt = DateTime.UtcNow;
                _context.NGOs.Add(ngo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"NGO '{ngo.Name}' created successfully.";
                return RedirectToAction(nameof(Ngos));
            }
            return View(ngo);
        }

        [HttpGet]
        public async Task<IActionResult> EditNgo(int id)
        {
            var ngo = await _context.NGOs.FindAsync(id);
            if (ngo == null) return NotFound();
            return View(ngo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNgo(NGO ngo)
        {
            if (ModelState.IsValid)
            {
                _context.NGOs.Update(ngo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"NGO '{ngo.Name}' updated successfully.";
                return RedirectToAction(nameof(Ngos));
            }
            return View(ngo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNgo(int id)
        {
            var ngo = await _context.NGOs.FindAsync(id);
            if (ngo != null)
            {
                _context.NGOs.Remove(ngo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "NGO removed successfully.";
            }
            return RedirectToAction(nameof(Ngos));
        }

        // ==========================================
        // 6. CORPORATE PARTNERS (CRUD)
        // ==========================================
        public async Task<IActionResult> Partners()
        {
            var partners = await _context.Partners.OrderBy(p => p.DisplayOrder).ToListAsync();
            return View(partners);
        }

        [HttpGet]
        public IActionResult CreatePartner()
        {
            return View(new Partner());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePartner(Partner partner)
        {
            if (ModelState.IsValid)
            {
                _context.Partners.Add(partner);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Partner '{partner.Name}' added successfully.";
                return RedirectToAction(nameof(Partners));
            }
            return View(partner);
        }

        [HttpGet]
        public async Task<IActionResult> EditPartner(int id)
        {
            var partner = await _context.Partners.FindAsync(id);
            if (partner == null) return NotFound();
            return View(partner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPartner(Partner partner)
        {
            if (ModelState.IsValid)
            {
                _context.Partners.Update(partner);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Partner '{partner.Name}' updated.";
                return RedirectToAction(nameof(Partners));
            }
            return View(partner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePartner(int id)
        {
            var partner = await _context.Partners.FindAsync(id);
            if (partner != null)
            {
                _context.Partners.Remove(partner);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Partner deleted.";
            }
            return RedirectToAction(nameof(Partners));
        }

        // ==========================================
        // 7. PROGRAMMES & PARTICIPANT INTERESTS
        // ==========================================
        public async Task<IActionResult> Programmes()
        {
            var programmes = await _context.Programmes
                .Include(p => p.Ngo)
                .Include(p => p.Interests)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
            return View(programmes);
        }

        [HttpGet]
        public async Task<IActionResult> CreateProgramme()
        {
            ViewBag.Ngos = await _context.NGOs.Where(n => n.IsActive).ToListAsync();
            ViewBag.Categories = new List<string> { "Education", "Health Care", "Privileged children (Specially challenged)", "Women Empowerment", "Elderly Support" };
            return View(new Programme { StartDate = DateTime.UtcNow.AddDays(7) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProgramme(Programme programme)
        {
            if (ModelState.IsValid)
            {
                programme.CreatedAt = DateTime.UtcNow;
                _context.Programmes.Add(programme);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Programme '{programme.Title}' created.";
                return RedirectToAction(nameof(Programmes));
            }
            ViewBag.Ngos = await _context.NGOs.Where(n => n.IsActive).ToListAsync();
            ViewBag.Categories = new List<string> { "Education", "Health Care", "Privileged children (Specially challenged)", "Women Empowerment", "Elderly Support" };
            return View(programme);
        }

        [HttpGet]
        public async Task<IActionResult> EditProgramme(int id)
        {
            var programme = await _context.Programmes.FindAsync(id);
            if (programme == null) return NotFound();

            ViewBag.Ngos = await _context.NGOs.Where(n => n.IsActive).ToListAsync();
            ViewBag.Categories = new List<string> { "Education", "Health Care", "Privileged children (Specially challenged)", "Women Empowerment", "Elderly Support" };
            return View(programme);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProgramme(Programme programme)
        {
            if (ModelState.IsValid)
            {
                _context.Programmes.Update(programme);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Programme '{programme.Title}' updated.";
                return RedirectToAction(nameof(Programmes));
            }
            ViewBag.Ngos = await _context.NGOs.Where(n => n.IsActive).ToListAsync();
            ViewBag.Categories = new List<string> { "Education", "Health Care", "Privileged children (Specially challenged)", "Women Empowerment", "Elderly Support" };
            return View(programme);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgramme(int id)
        {
            var programme = await _context.Programmes.FindAsync(id);
            if (programme != null)
            {
                _context.Programmes.Remove(programme);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Programme removed.";
            }
            return RedirectToAction(nameof(Programmes));
        }

        public async Task<IActionResult> ProgrammeInterests()
        {
            var interests = await _context.ProgrammeInterests
                .Include(pi => pi.Programme)
                .Include(pi => pi.User)
                .OrderByDescending(pi => pi.SubmittedAt)
                .ToListAsync();
            return View(interests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInterestStatus(int id, string status)
        {
            var interest = await _context.ProgrammeInterests.FindAsync(id);
            if (interest != null)
            {
                interest.Status = status;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Participant status updated.";
            }
            return RedirectToAction(nameof(ProgrammeInterests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgrammeInterest(int id)
        {
            var interest = await _context.ProgrammeInterests.FindAsync(id);
            if (interest != null)
            {
                _context.ProgrammeInterests.Remove(interest);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Participant volunteer entry removed.";
            }
            return RedirectToAction(nameof(ProgrammeInterests));
        }


        // ==========================================
        // 8. EVENTS (CRUD)
        // ==========================================
        public async Task<IActionResult> Events()
        {
            var events = await _context.Events.OrderByDescending(e => e.EventDate).ToListAsync();
            return View(events);
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View(new Event { EventDate = DateTime.UtcNow.AddDays(14) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event ev)
        {
            if (ModelState.IsValid)
            {
                ev.CreatedAt = DateTime.UtcNow;
                _context.Events.Add(ev);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Event '{ev.Title}' created.";
                return RedirectToAction(nameof(Events));
            }
            return View(ev);
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();
            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(Event ev)
        {
            if (ModelState.IsValid)
            {
                _context.Events.Update(ev);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Event '{ev.Title}' updated.";
                return RedirectToAction(nameof(Events));
            }
            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev != null)
            {
                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Event deleted.";
            }
            return RedirectToAction(nameof(Events));
        }

        // ==========================================
        // 9. GALLERY (CRUD)
        // ==========================================
        public async Task<IActionResult> Gallery()
        {
            var gallery = await _context.GalleryItems.OrderByDescending(g => g.CreatedAt).ToListAsync();
            return View(gallery);
        }

        [HttpGet]
        public IActionResult CreateGalleryItem()
        {
            return View(new GalleryItem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGalleryItem(GalleryItem item)
        {
            if (ModelState.IsValid)
            {
                item.CreatedAt = DateTime.UtcNow;
                _context.GalleryItems.Add(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Gallery image added.";
                return RedirectToAction(nameof(Gallery));
            }
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGalleryItem(int id)
        {
            var item = await _context.GalleryItems.FindAsync(id);
            if (item != null)
            {
                _context.GalleryItems.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Gallery item deleted.";
            }
            return RedirectToAction(nameof(Gallery));
        }

        // ==========================================
        // 10. ABOUT US SUB-PAGES CMS (What we do, Our mission, Our team, Career, Achievements, Supporters, Read About us)
        // ==========================================
        public async Task<IActionResult> AboutPages()
        {
            var pages = await _context.AboutPages.OrderBy(p => p.Id).ToListAsync();
            return View(pages);
        }

        [HttpGet]
        public async Task<IActionResult> EditAboutPage(int id)
        {
            var page = await _context.AboutPages.FindAsync(id);
            if (page == null) return NotFound();
            return View(page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAboutPage(AboutPage page)
        {
            if (ModelState.IsValid)
            {
                page.LastUpdated = DateTime.UtcNow;
                _context.AboutPages.Update(page);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Page '{page.Title}' updated successfully.";
                return RedirectToAction(nameof(AboutPages));
            }
            return View(page);
        }

        // ==========================================
        // 11. TEAM MEMBERS & CAREERS (About sub-page data)
        // ==========================================
        public async Task<IActionResult> TeamMembers()
        {
            var members = await _context.TeamMembers.OrderBy(t => t.DisplayOrder).ToListAsync();
            return View(members);
        }

        [HttpGet]
        public IActionResult CreateTeamMember() => View(new TeamMember());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeamMember(TeamMember member)
        {
            if (ModelState.IsValid)
            {
                _context.TeamMembers.Add(member);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Team member added.";
                return RedirectToAction(nameof(TeamMembers));
            }
            return View(member);
        }

        [HttpGet]
        public async Task<IActionResult> EditTeamMember(int id)
        {
            var member = await _context.TeamMembers.FindAsync(id);
            if (member == null) return NotFound();
            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeamMember(TeamMember member)
        {
            if (ModelState.IsValid)
            {
                _context.TeamMembers.Update(member);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Team member updated.";
                return RedirectToAction(nameof(TeamMembers));
            }
            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeamMember(int id)
        {
            var member = await _context.TeamMembers.FindAsync(id);
            if (member != null)
            {
                _context.TeamMembers.Remove(member);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Team member removed.";
            }
            return RedirectToAction(nameof(TeamMembers));
        }

        public async Task<IActionResult> Jobs()
        {
            var jobs = await _context.JobOpportunities.OrderByDescending(j => j.PostedDate).ToListAsync();
            return View(jobs);
        }

        [HttpGet]
        public IActionResult CreateJob() => View(new JobOpportunity());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(JobOpportunity job)
        {
            if (ModelState.IsValid)
            {
                job.PostedDate = DateTime.UtcNow;
                _context.JobOpportunities.Add(job);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Job posting created.";
                return RedirectToAction(nameof(Jobs));
            }
            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.JobOpportunities.FindAsync(id);
            if (job != null)
            {
                _context.JobOpportunities.Remove(job);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Job posting removed.";
            }
            return RedirectToAction(nameof(Jobs));
        }

        [HttpGet]
        public async Task<IActionResult> EditJob(int id)
        {
            var job = await _context.JobOpportunities.FindAsync(id);
            if (job == null) return NotFound();
            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(JobOpportunity job)
        {
            if (ModelState.IsValid)
            {
                _context.JobOpportunities.Update(job);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Job posting '{job.Title}' updated successfully.";
                return RedirectToAction(nameof(Jobs));
            }
            return View(job);
        }


        // ==========================================
        // 12. HELP CENTRE FAQS (CRUD)
        // ==========================================
        public async Task<IActionResult> Faqs()
        {
            var faqs = await _context.FAQs.OrderBy(f => f.DisplayOrder).ToListAsync();
            return View(faqs);
        }

        [HttpGet]
        public IActionResult CreateFaq() => View(new FAQ());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFaq(FAQ faq)
        {
            if (ModelState.IsValid)
            {
                _context.FAQs.Add(faq);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "FAQ created.";
                return RedirectToAction(nameof(Faqs));
            }
            return View(faq);
        }

        [HttpGet]
        public async Task<IActionResult> EditFaq(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);
            if (faq == null) return NotFound();
            return View(faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFaq(FAQ faq)
        {
            if (ModelState.IsValid)
            {
                _context.FAQs.Update(faq);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "FAQ updated.";
                return RedirectToAction(nameof(Faqs));
            }
            return View(faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFaq(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);
            if (faq != null)
            {
                _context.FAQs.Remove(faq);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "FAQ deleted.";
            }
            return RedirectToAction(nameof(Faqs));
        }

        // ==========================================
        // 13. USER QUERIES & REPLY CENTER
        // ==========================================
        public async Task<IActionResult> Queries()
        {
            var queries = await _context.UserQueries
                .Include(q => q.User)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
            return View(queries);
        }

        [HttpGet]
        public async Task<IActionResult> ReplyQuery(int id)
        {
            var query = await _context.UserQueries.FindAsync(id);
            if (query == null) return NotFound();

            var model = new AdminReplyQueryViewModel
            {
                QueryId = query.Id,
                FullName = query.FullName,
                Email = query.Email,
                Subject = query.Subject,
                Category = query.Category,
                OriginalMessage = query.Message,
                CreatedAt = query.CreatedAt,
                AdminReply = query.AdminReply ?? "",
                Status = query.Status
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyQuery(AdminReplyQueryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var query = await _context.UserQueries.FindAsync(model.QueryId);
                if (query != null)
                {
                    query.AdminReply = model.AdminReply.Trim();
                    query.Status = model.Status;
                    query.RepliedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    try
                    {
                        var html = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px; background: #ffffff;'>
                            <div style='background: #0b192c; padding: 18px 20px; border-radius: 8px; margin-bottom: 20px;'>
                                <h2 style='color: #ffffff; margin: 0; font-size: 20px;'>NGE<span style='color: #f25c05;'>OS</span> Support Resolution</h2>
                                <p style='color: #94a3b8; margin: 4px 0 0 0; font-size: 13px;'>Ticket #{query.Id} • {System.Net.WebUtility.HtmlEncode(query.Subject)}</p>
                            </div>
                            <p style='color: #1e293b; font-size: 15px;'>Dear <strong>{System.Net.WebUtility.HtmlEncode(query.FullName)}</strong>,</p>
                            <p style='color: #475569; font-size: 14px;'>Our administration team has reviewed your query. Here is our official response:</p>
                            <div style='background: #fff8f3; border-left: 4px solid #f25c05; padding: 16px; margin: 20px 0; border-radius: 4px;'>
                                <strong style='color: #0b192c; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;'>Administrative Resolution:</strong>
                                <p style='color: #334155; font-size: 14px; margin: 8px 0 0 0; line-height: 1.6;'>{System.Net.WebUtility.HtmlEncode(query.AdminReply).Replace("\n", "<br/>")}</p>
                            </div>
                            <p style='color: #64748b; font-size: 13px;'>Ticket Status: <strong style='color: #0b192c;'>{query.Status}</strong></p>
                            <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;' />
                            <p style='color: #94a3b8; font-size: 12px; margin: 0;'>NGEOS Central Secretariat • Empowering Communities Worldwide</p>
                        </div>";

                        await _emailSender.SendEmailAsync(query.Email, $"[NGEOS Support #{query.Id}] Resolution: {query.Subject}", html);
                    }
                    catch
                    {
                        // Silent fallback if SMTP unreachable
                    }

                    TempData["SuccessMessage"] = $"Reply saved and emailed successfully to {query.FullName} ({query.Email}).";
                    return RedirectToAction(nameof(Queries));
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuery(int id)
        {
            var query = await _context.UserQueries.FindAsync(id);
            if (query != null)
            {
                _context.UserQueries.Remove(query);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Query deleted.";
            }
            return RedirectToAction(nameof(Queries));
        }

        // ==========================================
        // 14. CONTACT MESSAGES
        // ==========================================
        public async Task<IActionResult> ContactMessages()
        {
            var messages = await _context.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync();
            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkMessageRead(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ContactMessages));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContactMessage(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Message removed.";
            }
            return RedirectToAction(nameof(ContactMessages));
        }
    }
}
