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
        public ObservableCollection<Event> UpcomingEvents { get; } = new();
        public ObservableCollection<Partner> CorporatePartners { get; } = new();

        public HomePortalViewModel(NgeosDbContext context, Action<string, object?> navigate)
        {
            _context = context;
            _navigate = navigate;
        }

        private bool _isLoading;

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
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

                UpcomingEvents.Clear();
                var evs = await _context.Events.Take(2).ToListAsync();
                foreach (var e in evs)
                {
                    UpcomingEvents.Add(e);
                }

                CorporatePartners.Clear();
                var pts = await _context.Partners.Take(4).ToListAsync();
                foreach (var pt in pts)
                {
                    CorporatePartners.Add(pt);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed loading homepage data: {ex.Message}";
            }
            finally
            {
                _isLoading = false;
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
        public void NavigateToCauseDetails(Cause? cause)
        {
            if (cause != null) _navigate("CauseDetails", cause);
        }

        [RelayCommand]
        public void NavigateToProgrammes()
        {
            _navigate("Programmes", null);
        }

        [RelayCommand]
        public void NavigateToProgrammeDetails(Programme? programme)
        {
            if (programme != null) _navigate("ProgrammeDetails", programme);
        }

        [RelayCommand]
        public void NavigateToEvents()
        {
            _navigate("Events", null);
        }

        [RelayCommand]
        public void NavigateToGallery()
        {
            _navigate("Gallery", null);
        }

        [RelayCommand]
        public void NavigateToNgos()
        {
            _navigate("Ngos", null);
        }

        [RelayCommand]
        public void NavigateToZakat()
        {
            _navigate("Zakat", null);
        }

        [RelayCommand]
        public void NavigateToAbout()
        {
            _navigate("About", null);
        }

        [RelayCommand]
        public void NavigateToHelpCentre()
        {
            _navigate("HelpCentre", null);
        }

        [RelayCommand]
        public void NavigateToContact()
        {
            _navigate("Contact", null);
        }
    }

    // ==========================================
    // 2. CAUSES CATALOG VIEW MODEL (PUBLIC DIRECTORY)
    // ==========================================
    public partial class CausesCatalogViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly Action<string, object?> _navigate;

        public ObservableCollection<Cause> CausesList { get; } = new();
        public ObservableCollection<string> Categories { get; } = new() 
        { 
            "All Categories", "Education", "Water Relief", "Healthcare", "Livelihood", "Orphan Care" 
        };

        [ObservableProperty]
        private string _selectedCategory = "All Categories";

        [ObservableProperty]
        private string _searchText = string.Empty;

        public CausesCatalogViewModel(NgeosDbContext context, Action<string, object?> navigate)
        {
            _context = context;
            _navigate = navigate;
        }

        partial void OnSelectedCategoryChanged(string value) => _ = FilterCausesAsync();
        partial void OnSearchTextChanged(string value) => _ = FilterCausesAsync();

        [RelayCommand]
        public async Task LoadAsync()
        {
            await FilterCausesAsync();
        }

        private async Task FilterCausesAsync()
        {
            try
            {
                var query = _context.Causes.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All Categories")
                {
                    query = query.Where(c => c.Category == SelectedCategory);
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var term = SearchText.Trim().ToLower();
                    query = query.Where(c => c.Title.ToLower().Contains(term) || c.Description.ToLower().Contains(term));
                }

                var list = await query.ToListAsync();
                CausesList.Clear();
                foreach (var c in list)
                {
                    CausesList.Add(c);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed loading causes: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ViewCauseDetails(Cause? cause)
        {
            if (cause != null) _navigate("CauseDetails", cause);
        }

        [RelayCommand]
        public void DonateNow(Cause? cause)
        {
            _navigate("Donate", cause);
        }
    }

    // ==========================================
    // 3. CAUSE DETAILS VIEW MODEL (IN-DEPTH STORY)
    // ==========================================
    public partial class CauseDetailsViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly Action<string, object?> _navigate;

        [ObservableProperty]
        private Cause? _currentCause;

        [ObservableProperty]
        private int _donorCount;

        [ObservableProperty]
        private decimal _customDonateAmount = 100m;

        public ObservableCollection<Donation> CauseDonors { get; } = new();

        public CauseDetailsViewModel(NgeosDbContext context, Action<string, object?> navigate)
        {
            _context = context;
            _navigate = navigate;
        }

        [RelayCommand]
        public async Task LoadAsync(Cause cause)
        {
            CurrentCause = cause;
            try
            {
                var donations = await _context.Donations
                    .Where(d => d.CauseId == cause.Id && d.PaymentStatus == "Completed")
                    .OrderByDescending(d => d.DonationDate)
                    .ToListAsync();

                DonorCount = donations.Count;
                CauseDonors.Clear();
                foreach (var d in donations.Take(8))
                {
                    CauseDonors.Add(d);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Could not load cause details: {ex.Message}";
            }
        }

        [RelayCommand]
        public void SelectAmount(string amtStr)
        {
            if (decimal.TryParse(amtStr, out var amt))
            {
                CustomDonateAmount = amt;
            }
        }

        [RelayCommand]
        public void DonateToThisCause()
        {
            _navigate("Donate", CurrentCause);
        }

        [RelayCommand]
        public void BackToCatalog()
        {
            _navigate("Causes", null);
        }
    }

    // ==========================================
    // 4. PUBLIC DONATE VIEW MODEL (CHECKOUT / PAYMENT)
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
        private string _selectedGateway = "Safepay";

        [ObservableProperty]
        private string _paymentMethod = "Safepay (Visa / MasterCard / PayPak)";

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
                CustomAmountText = amt.ToString("F0");
            }
        }

        [RelayCommand]
        public void SelectDonationType(string type)
        {
            DonationType = type;
        }

        [RelayCommand]
        public void SelectGateway(string gateway)
        {
            if (string.IsNullOrWhiteSpace(gateway)) return;
            SelectedGateway = gateway;
            PaymentMethod = gateway switch
            {
                "Safepay" => "Safepay (Visa / MasterCard / PayPak)",
                "JazzCash" => "JazzCash Mobile Account / Voucher",
                "Easypaisa" => "Easypaisa Mobile Wallet",
                "PayFast" => "PayFast (1Link Interbank / All Banks)",
                "Stripe" => "Stripe (International Credit / Debit Card)",
                _ => gateway
            };
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

                // Add to Sync Queue
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
                    // Non-fatal if PDF viewer/export is interrupted
                }

                await _auditService.LogAsync(
                    "DonationCreated",
                    "Donation",
                    donation.Id.ToString(),
                    $"Donation of {Amount:C} processed via {PaymentMethod} for {SelectedCause.Title}. Receipt: {receiptNum}");

                SuccessMessage = $"Thank you! Your contribution of ${Amount:N2} has been recorded successfully in local SQLite and queued for multi-device sync! Receipt #{receiptNum} is ready.";
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
    // 5. PROGRAMMES CATALOG VIEW MODEL
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
        public void ViewProgrammeDetails(Programme? programme)
        {
            if (programme != null) _navigate("ProgrammeDetails", programme);
        }

        [RelayCommand]
        public void SupportProgramme(Programme? programme)
        {
            _navigate("Donate", null);
        }
    }

    // ==========================================
    // 6. PROGRAMME DETAILS VIEW MODEL (VOLUNTEER SIGNUP)
    // ==========================================
    public partial class ProgrammeDetailsViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly Action<string, object?> _navigate;
        private readonly IAuditService _auditService;

        [ObservableProperty]
        private Programme? _currentProgramme;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _occupation = "Student / Professional";

        [ObservableProperty]
        private string _interestType = "Volunteer"; // Volunteer, Participant, Sponsor

        [ObservableProperty]
        private string _notes = string.Empty;

        public ProgrammeDetailsViewModel(NgeosDbContext context, Action<string, object?> navigate, IAuditService auditService)
        {
            _context = context;
            _navigate = navigate;
            _auditService = auditService;
        }

        [RelayCommand]
        public void Load(Programme programme)
        {
            ClearMessages();
            CurrentProgramme = programme;
        }

        [RelayCommand]
        public async Task RegisterInterestAsync()
        {
            ClearMessages();
            if (CurrentProgramme == null) return;

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

            IsBusy = true;
            try
            {
                var interest = new ProgrammeInterest
                {
                    ProgrammeId = CurrentProgramme.Id,
                    FullName = FullName.Trim(),
                    Email = Email.Trim().ToLower(),
                    Phone = Phone?.Trim() ?? string.Empty,
                    Occupation = Occupation,
                    InterestType = InterestType,
                    Message = Notes?.Trim(),
                    Status = "Pending",
                    SubmittedAt = DateTime.UtcNow
                };

                await _context.ProgrammeInterests.AddAsync(interest);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("ProgrammeInterestRegistered", "ProgrammeInterest", interest.Id.ToString(), $"Registered for {CurrentProgramme.Title} as {InterestType}");

                SuccessMessage = $"Thank you, {FullName}! Your interest in '{CurrentProgramme.Title}' as a {InterestType} has been successfully registered. Our team will contact you.";
                Notes = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to register interest: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void BackToProgrammes()
        {
            _navigate("Programmes", null);
        }
    }

    // ==========================================
    // 7. EVENTS VIEW MODEL (COMMUNITY & RELIEF DRIVES)
    // ==========================================
    public partial class EventsViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly Action<string, object?> _navigate;

        public ObservableCollection<Event> EventsList { get; } = new();

        public EventsViewModel(NgeosDbContext context, Action<string, object?> navigate)
        {
            _context = context;
            _navigate = navigate;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            try
            {
                EventsList.Clear();
                var evs = await _context.Events.OrderBy(e => e.EventDate).ToListAsync();
                foreach (var e in evs)
                {
                    EventsList.Add(e);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load events: {ex.Message}";
            }
        }

        [RelayCommand]
        public void RsvpEvent(Event? ev)
        {
            if (ev != null)
            {
                SuccessMessage = $"RSVP Confirmed for '{ev.Title}'! We look forward to seeing you at {ev.Location} on {ev.EventDate:MMM dd, yyyy}.";
            }
        }
    }

    // ==========================================
    // 8. GALLERY VIEW MODEL (FIELD IMPACT MEDIA)
    // ==========================================
    public partial class GalleryViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;

        public ObservableCollection<GalleryItem> Items { get; } = new();
        public ObservableCollection<string> Categories { get; } = new() { "All Categories", "Relief", "Education", "Healthcare", "Community" };

        [ObservableProperty]
        private string _selectedCategory = "All Categories";

        public GalleryViewModel(NgeosDbContext context)
        {
            _context = context;
        }

        partial void OnSelectedCategoryChanged(string value) => _ = LoadAsync();

        [RelayCommand]
        public async Task LoadAsync()
        {
            try
            {
                var query = _context.GalleryItems.AsQueryable();
                if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All Categories")
                {
                    query = query.Where(g => g.Category == SelectedCategory);
                }

                var list = await query.ToListAsync();
                Items.Clear();
                foreach (var item in list)
                {
                    Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed loading gallery: {ex.Message}";
            }
        }
    }

    // ==========================================
    // 9. ACCREDITED NGOS DIRECTORY VIEW MODEL
    // ==========================================
    public partial class NgosDirectoryViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly Action<string, object?> _navigate;

        public ObservableCollection<NGO> NgosList { get; } = new();

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        public NgosDirectoryViewModel(NgeosDbContext context, Action<string, object?> navigate)
        {
            _context = context;
            _navigate = navigate;
        }

        partial void OnSearchQueryChanged(string value) => _ = FilterNgosAsync();

        [RelayCommand]
        public async Task LoadAsync()
        {
            await FilterNgosAsync();
        }

        private async Task FilterNgosAsync()
        {
            try
            {
                var query = _context.NGOs.Where(n => n.IsActive);
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    var term = SearchQuery.Trim().ToLower();
                    query = query.Where(n => n.Name.ToLower().Contains(term) || n.FocusArea.ToLower().Contains(term));
                }

                var list = await query.ToListAsync();
                NgosList.Clear();
                foreach (var ngo in list)
                {
                    NgosList.Add(ngo);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed loading NGOs: {ex.Message}";
            }
        }

        [RelayCommand]
        public void SupportNgo(NGO? ngo)
        {
            _navigate("Donate", null);
        }
    }

    // ==========================================
    // 10. HELP CENTRE & FAQS VIEW MODEL
    // ==========================================
    public partial class HelpCentreFaqViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly Action<string, object?> _navigate;

        public ObservableCollection<FAQ> FaqList { get; } = new();
        public ObservableCollection<string> Categories { get; } = new() 
        { 
            "All Categories", "General", "Donations & Payment", "Tax & Receipts", "NGO Partnership" 
        };

        [ObservableProperty]
        private string _selectedCategory = "All Categories";

        [ObservableProperty]
        private string _searchFilter = string.Empty;

        public HelpCentreFaqViewModel(NgeosDbContext context, Action<string, object?> navigate)
        {
            _context = context;
            _navigate = navigate;
        }

        partial void OnSelectedCategoryChanged(string value) => _ = FilterFaqsAsync();
        partial void OnSearchFilterChanged(string value) => _ = FilterFaqsAsync();

        [RelayCommand]
        public async Task LoadAsync()
        {
            await FilterFaqsAsync();
        }

        private async Task FilterFaqsAsync()
        {
            try
            {
                var query = _context.FAQs.Where(f => f.IsActive);
                if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All Categories")
                {
                    query = query.Where(f => f.Category == SelectedCategory);
                }

                if (!string.IsNullOrWhiteSpace(SearchFilter))
                {
                    var term = SearchFilter.Trim().ToLower();
                    query = query.Where(f => f.Question.ToLower().Contains(term) || f.Answer.ToLower().Contains(term));
                }

                var list = await query.OrderBy(f => f.DisplayOrder).ToListAsync();
                FaqList.Clear();
                foreach (var f in list)
                {
                    FaqList.Add(f);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed loading FAQs: {ex.Message}";
            }
        }

        [RelayCommand]
        public void NavigateToContactForm()
        {
            _navigate("Contact", null);
        }
    }

    // ==========================================
    // 11. CITIZEN USER PORTAL / DASHBOARD VIEW MODEL
    // ==========================================
    public partial class UserPortalViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;
        private readonly IReportService _reportService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditService _auditService;

        [ObservableProperty]
        private User? _activeUser;

        [ObservableProperty]
        private string _selectedTab = "Overview"; // Overview, Donations, Programmes, Queries, Invite

        // Profile fields
        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _profession = string.Empty;

        [ObservableProperty]
        private string _organization = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        // Metrics
        [ObservableProperty]
        private decimal _userTotalDonated;

        [ObservableProperty]
        private int _userDonationsCount;

        [ObservableProperty]
        private int _userProgrammesCount;

        [ObservableProperty]
        private int _userQueriesCount;

        // Friend Invite
        [ObservableProperty]
        private string _friendName = string.Empty;

        [ObservableProperty]
        private string _friendEmail = string.Empty;

        [ObservableProperty]
        private string _inviteMessage = "Join me on NGEOS to support transparent humanitarian causes worldwide!";

        public ObservableCollection<Donation> UserDonations { get; } = new();
        public ObservableCollection<ProgrammeInterest> UserProgrammes { get; } = new();
        public ObservableCollection<Query> UserQueries { get; } = new();

        public UserPortalViewModel(
            NgeosDbContext context,
            IReportService reportService,
            IPasswordHasher passwordHasher,
            IAuditService auditService)
        {
            _context = context;
            _reportService = reportService;
            _passwordHasher = passwordHasher;
            _auditService = auditService;
        }

        [RelayCommand]
        public async Task LoadAsync(User user)
        {
            ClearMessages();
            ActiveUser = user;
            FullName = user.FullName;
            Email = user.Email;
            Profession = user.Profession ?? string.Empty;
            Organization = user.Organization ?? string.Empty;
            Phone = user.Phone ?? string.Empty;

            try
            {
                var donations = await _context.Donations
                    .Where(d => d.DonorEmail.ToLower() == user.Email.ToLower())
                    .OrderByDescending(d => d.DonationDate)
                    .ToListAsync();

                UserDonations.Clear();
                foreach (var d in donations) UserDonations.Add(d);
                UserTotalDonated = donations.Sum(d => d.Amount);
                UserDonationsCount = donations.Count;

                var interests = await _context.ProgrammeInterests
                    .Where(p => p.Email.ToLower() == user.Email.ToLower() || p.UserId == user.Id)
                    .OrderByDescending(p => p.SubmittedAt)
                    .ToListAsync();

                UserProgrammes.Clear();
                foreach (var pi in interests) UserProgrammes.Add(pi);
                UserProgrammesCount = interests.Count;

                var queries = await _context.Queries
                    .Where(q => q.Email.ToLower() == user.Email.ToLower() || q.UserId == user.Id)
                    .OrderByDescending(q => q.CreatedAt)
                    .ToListAsync();

                UserQueries.Clear();
                foreach (var q in queries) UserQueries.Add(q);
                UserQueriesCount = queries.Count;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed loading user dashboard: {ex.Message}";
            }
        }

        [RelayCommand]
        public void SelectTab(string tab)
        {
            SelectedTab = tab;
            ClearMessages();
        }

        [RelayCommand]
        public async Task UpdateProfileAsync()
        {
            ClearMessages();
            if (ActiveUser == null) return;

            try
            {
                ActiveUser.FullName = FullName.Trim();
                ActiveUser.Profession = Profession?.Trim();
                ActiveUser.Organization = Organization?.Trim();
                ActiveUser.Phone = Phone?.Trim();

                if (!string.IsNullOrWhiteSpace(NewPassword))
                {
                    var (h, s) = _passwordHasher.HashPassword(NewPassword);
                    ActiveUser.PasswordHash = h;
                    ActiveUser.PasswordSalt = s;
                }

                await _context.SaveChangesAsync();
                await _auditService.LogAsync("UserProfileUpdated", "User", ActiveUser.Id.ToString(), $"Profile updated for {ActiveUser.Email}");

                SuccessMessage = "Profile updated successfully in local database!";
                NewPassword = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed updating profile: {ex.Message}";
            }
        }

        [RelayCommand]
        public void DownloadReceipt(Donation? donation)
        {
            if (donation == null) return;
            try
            {
                var pdfBytes = _reportService.GenerateDonationReceiptPdf(donation);
                var receiptsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NGEOS_Receipts");
                if (!Directory.Exists(receiptsDir)) Directory.CreateDirectory(receiptsDir);

                var filePath = Path.Combine(receiptsDir, $"{donation.ReceiptNumber}.pdf");
                File.WriteAllBytes(filePath, pdfBytes);

                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                SuccessMessage = $"Official PDF Receipt #{donation.ReceiptNumber} generated and opened!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Could not download receipt: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task SendFriendInvitationAsync()
        {
            ClearMessages();
            if (ActiveUser == null) return;

            if (string.IsNullOrWhiteSpace(FriendName))
            {
                ErrorMessage = "Please enter your friend's name.";
                return;
            }

            if (string.IsNullOrWhiteSpace(FriendEmail) || !FriendEmail.Contains("@"))
            {
                ErrorMessage = "Please provide a valid friend email address.";
                return;
            }

            try
            {
                var invite = new FriendInvitation
                {
                    SenderUserId = ActiveUser.Id,
                    FriendName = FriendName.Trim(),
                    FriendEmail = FriendEmail.Trim().ToLower(),
                    InvitationMessage = InviteMessage?.Trim() ?? string.Empty,
                    SentAt = DateTime.UtcNow
                };

                await _context.FriendInvitations.AddAsync(invite);
                await _context.SaveChangesAsync();

                SuccessMessage = $"Invitation recorded for {FriendName} ({FriendEmail})! Thank you for sharing NGEOS.";
                FriendName = string.Empty;
                FriendEmail = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed sending invitation: {ex.Message}";
            }
        }
    }

    // ==========================================
    // 12. ZAKAT CALCULATOR VIEW MODEL
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
    // 13. ABOUT VIEW MODEL (STORY, TEAM, PARTNERS, CAREERS)
    // ==========================================
    public partial class AboutViewModel : ViewModelBase
    {
        private readonly NgeosDbContext _context;

        public string OrganizationName => "NGEOS Humanitarian Welfare Platform";
        public string Tagline => "A Unified Bridge Connecting Generosity with Genuine Needs Worldwide";
        public string EstablishedYear => "2024 - 2026";
        public string TransparencyRate => "100%";
        public string AuditStandard => "ISO 9001 Certified & Shariah-Compliant Zakat Allocation";

        [ObservableProperty]
        private string _selectedSection = "Story"; // Story, Team, Supporters, Careers

        public ObservableCollection<TeamMember> TeamMembers { get; } = new();
        public ObservableCollection<Partner> CorporatePartners { get; } = new();
        public ObservableCollection<JobOpportunity> JobOpenings { get; } = new();

        public AboutViewModel(NgeosDbContext context)
        {
            _context = context;
        }

        [RelayCommand]
        public void SelectSection(string section)
        {
            SelectedSection = section;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            try
            {
                TeamMembers.Clear();
                var team = await _context.TeamMembers.Where(t => t.IsActive).OrderBy(t => t.DisplayOrder).ToListAsync();
                foreach (var m in team) TeamMembers.Add(m);

                CorporatePartners.Clear();
                var pts = await _context.Partners.Where(p => p.IsActive).ToListAsync();
                foreach (var p in pts) CorporatePartners.Add(p);

                JobOpenings.Clear();
                var jobs = await _context.JobOpportunities.Where(j => j.IsActive).ToListAsync();
                foreach (var j in jobs) JobOpenings.Add(j);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed loading about page details: {ex.Message}";
            }
        }
    }

    // ==========================================
    // 14. CONTACT VIEW MODEL (QUERIES & HELPDESK)
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

                SuccessMessage = "Thank you! Your humanitarian inquiry has been securely recorded. Our support team will respond shortly.";
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
