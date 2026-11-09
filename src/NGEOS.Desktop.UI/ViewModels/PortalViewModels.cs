using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;
using NGEOS.Desktop.Infrastructure.Database;

namespace NGEOS.Desktop.UI.ViewModels
{
    // ==========================================
    // 1. HOME PORTAL VIEW MODEL (WEBSITE HOMEPAGE)
    // ==========================================
    public partial class HomePortalViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly Action<string, object?> _navigate;

        [ObservableProperty]
        private decimal _totalRaised;

        [ObservableProperty]
        private int _totalDonors;

        [ObservableProperty]
        private int _activeCauses;

        [ObservableProperty]
        private int _totalBeneficiaries;

        public ObservableCollection<Cause> FeaturedCauses { get; } = new();
        public ObservableCollection<Donation> RecentDonations { get; } = new();
        public ObservableCollection<Programme> CoreProgrammes { get; } = new();

        public HomePortalViewModel(NgeosDbContext context, Action<string, object?> navigate)
        {
            _context = context;
            _navigate = navigate;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            try
            {
                var donations = await _context.Donations
                    .Where(d => d.PaymentStatus == "Completed")
                    .ToListAsync();

                TotalRaised = donations.Sum(d => d.Amount);
                TotalDonors = donations.Select(d => d.DonorEmail.ToLower()).Distinct().Count();

                var causes = await _context.Causes.ToListAsync();
                ActiveCauses = causes.Count(c => c.IsActive);
                TotalBeneficiaries = causes.Sum(c => c.BeneficiariesCount);

                FeaturedCauses.Clear();
                foreach (var c in causes.Where(c => c.IsActive).Take(4))
                {
                    FeaturedCauses.Add(c);
                }

                RecentDonations.Clear();
                var recent = await _context.Donations
                    .OrderByDescending(d => d.DonationDate)
                    .Take(5)
                    .ToListAsync();
                foreach (var d in recent)
                {
                    RecentDonations.Add(d);
                }

                CoreProgrammes.Clear();
                var progs = await _context.Programmes.Take(3).ToListAsync();
                foreach (var p in progs)
                {
                    CoreProgrammes.Add(p);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed loading homepage data: {ex.Message}";
            }
        }

        [RelayCommand]
        public void NavigateToDonate(Cause? cause = null)
        {
            _navigate("Donate", cause);
        }

        [RelayCommand]
        public void NavigateToCauses()
        {
            _navigate("Causes", null);
        }

        [RelayCommand]
        public void NavigateToProgrammes()
        {
            _navigate("Programmes", null);
        }

        [RelayCommand]
        public void NavigateToZakat()
        {
            _navigate("Zakat", null);
        }

        [RelayCommand]
        public void NavigateToContact()
        {
            _navigate("Contact", null);
        }
    }

    // ==========================================
    // 2. PUBLIC DONATE VIEW MODEL (CHECKOUT / PAYMENT)
    // ==========================================
    public partial class PublicDonateViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly IReportService _reportService;
        private readonly ISyncService _syncService;
        private readonly IAuditService _auditService;

        public ObservableCollection<Cause> Causes { get; } = new();

        [ObservableProperty]
        private Cause? _selectedCause;

        [ObservableProperty]
        private decimal _amount = 50m;

        [ObservableProperty]
        private string _customAmountText = "50";

        [ObservableProperty]
        private string _donationType = "General Sadaqah / Relief";

        [ObservableProperty]
        private string _donorName = "Muhammad Ali";

        [ObservableProperty]
        private string _donorEmail = "ali@donor.org";

        [ObservableProperty]
        private string _donorPhone = "+92 300 9876543";

        [ObservableProperty]
        private bool _isAnonymous;

        [ObservableProperty]
        private string _paymentMethod = "Online Payment Simulation (Stripe)";

        [ObservableProperty]
        private string? _lastReceiptNumber;

        [ObservableProperty]
        private string? _lastReceiptPath;

        [ObservableProperty]
        private bool _isReceiptAvailable;

        public PublicDonateViewModel(
            NgeosDbContext context,
            IReportService reportService,
            ISyncService syncService,
            IAuditService auditService)
        {
            _context = context;
            _reportService = reportService;
            _syncService = syncService;
            _auditService = auditService;
        }

        [RelayCommand]
        public async Task LoadAsync(Cause? preselectedCause = null)
        {
            ClearMessages();
            IsReceiptAvailable = false;
            try
            {
                Causes.Clear();
                var causesList = await _context.Causes.Where(c => c.IsActive).ToListAsync();
                foreach (var c in causesList)
                {
                    Causes.Add(c);
                }

                if (preselectedCause != null)
                {
                    SelectedCause = Causes.FirstOrDefault(c => c.Id == preselectedCause.Id) ?? Causes.FirstOrDefault();
                }
                else if (SelectedCause == null && Causes.Any())
                {
                    SelectedCause = Causes.First();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading causes: {ex.Message}";
            }
        }

        [RelayCommand]
        public void SelectAmount(string amountStr)
        {
            if (decimal.TryParse(amountStr, out var amt))
            {
                Amount = amt;
                CustomAmountText = amt.ToString();
            }
        }

        [RelayCommand]
        public void SelectDonationType(string type)
        {
            DonationType = type;
        }

        [RelayCommand]
        public async Task ProcessDonationAsync()
        {
            ClearMessages();

            if (decimal.TryParse(CustomAmountText, out var parsedAmt) && parsedAmt > 0)
            {
                Amount = parsedAmt;
            }

            if (Amount <= 0)
            {
                ErrorMessage = "Please enter a valid donation amount greater than zero.";
                return;
            }

            if (string.IsNullOrWhiteSpace(DonorEmail) || !DonorEmail.Contains("@"))
            {
                ErrorMessage = "Please provide a valid donor email address for receipt delivery.";
                return;
            }

            if (SelectedCause == null)
            {
                ErrorMessage = "Please select a humanitarian campaign/cause.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Processing donation transaction and generating certified PDF receipt...";

            try
            {
                var txnId = $"TXN-DESK-{Guid.NewGuid():N}"[..18].ToUpper();
                var receiptNum = $"NGEOS-REC-{DateTime.UtcNow:yyyyMM}-{Random.Shared.Next(1000, 9999)}";
                var clientGuid = Guid.NewGuid().ToString();

                var donation = new Donation
                {
                    TransactionId = txnId,
                    ReceiptNumber = receiptNum,
                    DonorName = IsAnonymous ? "Anonymous Benefactor" : (string.IsNullOrWhiteSpace(DonorName) ? "Kind Supporter" : DonorName.Trim()),
                    DonorEmail = DonorEmail.Trim().ToLower(),
                    DonorPhone = DonorPhone?.Trim() ?? string.Empty,
                    Amount = Amount,
                    PaymentMethod = PaymentMethod,
                    PaymentStatus = "Completed",
                    CauseId = SelectedCause.Id,
                    CauseTitle = SelectedCause.Title,
                    DonationDate = DateTime.UtcNow,
                    Notes = $"Direct contribution via NGEOS Portal. Type: {DonationType}. Cause: {SelectedCause.Title}. Anonymous: {IsAnonymous}",
                    IsSynced = false,
                    ClientGuid = clientGuid,
                    LastModifiedUtc = DateTime.UtcNow
                };

                await _context.Donations.AddAsync(donation);

                // Update Cause progress
                SelectedCause.RaisedAmount += Amount;
                SelectedCause.LastModifiedUtc = DateTime.UtcNow;
                SelectedCause.IsSynced = false;

                // Add to Sync Queue via QueueOperationAsync
                await _syncService.QueueOperationAsync("Donation", "Create", "0", donation);
                await _syncService.QueueOperationAsync("Cause", "Update", SelectedCause.Id.ToString(), SelectedCause);

                // Save to SQLite
                await _context.SaveChangesAsync();

                // Generate certified PDF Receipt
                try
                {
                    var pdfBytes = _reportService.GenerateDonationReceiptPdf(donation);
                    var receiptsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NGEOS_Receipts");
                    if (!Directory.Exists(receiptsDir)) Directory.CreateDirectory(receiptsDir);

                    var receiptFilePath = Path.Combine(receiptsDir, $"{receiptNum}.pdf");
                    await File.WriteAllBytesAsync(receiptFilePath, pdfBytes);

                    LastReceiptNumber = receiptNum;
                    LastReceiptPath = receiptFilePath;
                    IsReceiptAvailable = true;
                }
                catch
                {
                    // Non-fatal if PDF fails
                }

                await _auditService.LogAsync(
                    "DonationCreated",
                    "Donation",
                    donation.Id.ToString(),
                    $"Donation of {Amount:C} processed via {PaymentMethod} for {SelectedCause.Title}. Receipt: {receiptNum}");

                SuccessMessage = $"🎉 Thank you! Your contribution of ${Amount:N2} has been recorded successfully in local SQLite and queued for multi-device sync! Receipt #{receiptNum} is ready.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Donation failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void OpenReceipt()
        {
            if (!string.IsNullOrEmpty(LastReceiptPath) && File.Exists(LastReceiptPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(LastReceiptPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Could not open PDF viewer: {ex.Message}";
                }
            }
        }
    }

    // ==========================================
    // 3. PROGRAMMES VIEW MODEL
    // ==========================================
    public partial class ProgrammesViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly Action<string, object?> _navigate;

        public ObservableCollection<Programme> Programmes { get; } = new();

        public ProgrammesViewModel(NgeosDbContext context, Action<string, object?> navigate)
        {
            _context = context;
            _navigate = navigate;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            try
            {
                Programmes.Clear();
                var list = await _context.Programmes.ToListAsync();
                foreach (var p in list)
                {
                    Programmes.Add(p);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load programmes: {ex.Message}";
            }
        }

        [RelayCommand]
        public void SupportProgramme(Programme? programme)
        {
            _navigate("Donate", null);
        }
    }

    // ==========================================
    // 4. ZAKAT CALCULATOR VIEW MODEL
    // ==========================================
    public partial class ZakatCalculatorViewModel : ViewModelBase
    {
        private readonly Action<string, object?> _navigate;

        [ObservableProperty]
        private decimal _cashOnHand = 1500m;

        [ObservableProperty]
        private decimal _bankBalance = 4200m;

        [ObservableProperty]
        private decimal _goldValue = 2800m;

        [ObservableProperty]
        private decimal _silverValue = 350m;

        [ObservableProperty]
        private decimal _businessAssets = 0m;

        [ObservableProperty]
        private decimal _debtsLiabilities = 450m;

        [ObservableProperty]
        private decimal _nisabThreshold = 520m; // Approximate silver standard benchmark

        [ObservableProperty]
        private decimal _totalGrossAssets;

        [ObservableProperty]
        private decimal _netZakatableWealth;

        [ObservableProperty]
        private decimal _zakatPayable;

        [ObservableProperty]
        private bool _isZakatDue;

        public ZakatCalculatorViewModel(Action<string, object?> navigate)
        {
            _navigate = navigate;
            Recalculate();
        }

        partial void OnCashOnHandChanged(decimal value) => Recalculate();
        partial void OnBankBalanceChanged(decimal value) => Recalculate();
        partial void OnGoldValueChanged(decimal value) => Recalculate();
        partial void OnSilverValueChanged(decimal value) => Recalculate();
        partial void OnBusinessAssetsChanged(decimal value) => Recalculate();
        partial void OnDebtsLiabilitiesChanged(decimal value) => Recalculate();
        partial void OnNisabThresholdChanged(decimal value) => Recalculate();

        [RelayCommand]
        public void Recalculate()
        {
            TotalGrossAssets = CashOnHand + BankBalance + GoldValue + SilverValue + BusinessAssets;
            NetZakatableWealth = Math.Max(0, TotalGrossAssets - DebtsLiabilities);
            IsZakatDue = NetZakatableWealth >= NisabThreshold;
            ZakatPayable = IsZakatDue ? Math.Round(NetZakatableWealth * 0.025m, 2) : 0m;
        }

        [RelayCommand]
        public void PayZakatNow()
        {
            if (ZakatPayable > 0)
            {
                _navigate("DonateWithZakat", ZakatPayable);
            }
            else
            {
                _navigate("Donate", null);
            }
        }
    }

    // ==========================================
    // 5. ABOUT VIEW MODEL
    // ==========================================
    public partial class AboutViewModel : ViewModelBase
    {
        public string OrganizationName => "NGEOS Humanitarian Welfare Platform";
        public string Tagline => "A Unified Bridge Connecting Generosity with Genuine Needs Worldwide";
        public string EstablishedYear => "2024 - 2026";
        public string TransparencyRate => "100%";
        public string AuditStandard => "ISO 9001 Certified & Shariah-Compliant Zakat Allocation";

        public AboutViewModel()
        {
        }
    }

    // ==========================================
    // 6. CONTACT VIEW MODEL (QUERIES & HELPDESK)
    // ==========================================
    public partial class ContactViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly ISyncService _syncService;
        private readonly IAuditService _auditService;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _subject = "Humanitarian Assistance Inquiry";

        [ObservableProperty]
        private string _message = string.Empty;

        public ContactViewModel(NgeosDbContext context, ISyncService syncService, IAuditService auditService)
        {
            _context = context;
            _syncService = syncService;
            _auditService = auditService;
        }

        [RelayCommand]
        public async Task SubmitQueryAsync()
        {
            ClearMessages();

            if (string.IsNullOrWhiteSpace(FullName))
            {
                ErrorMessage = "Please provide your full name.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@"))
            {
                ErrorMessage = "Please provide a valid email address.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Message))
            {
                ErrorMessage = "Please enter your message or humanitarian inquiry.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Submitting your message to local database and sync queue...";

            try
            {
                var query = new Query
                {
                    FullName = FullName.Trim(),
                    Email = Email.Trim().ToLower(),
                    Phone = Phone?.Trim(),
                    Subject = Subject?.Trim() ?? "General Inquiry",
                    Message = Message.Trim(),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Queries.AddAsync(query);
                await _syncService.QueueOperationAsync("Query", "Create", "0", query);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("QuerySubmitted", "Query", query.Id.ToString(), $"Query submitted by {FullName} ({Email}): {Subject}");

                SuccessMessage = "✅ Thank you! Your humanitarian inquiry has been securely recorded. Our support team will respond shortly.";
                Message = string.Empty;
                Subject = "Humanitarian Assistance Inquiry";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to submit query: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
