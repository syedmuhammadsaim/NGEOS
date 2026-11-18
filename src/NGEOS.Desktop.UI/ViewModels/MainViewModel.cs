using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;

namespace NGEOS.Desktop.UI.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectivityService _connectivity;
        private readonly ISyncService _syncService;
        private readonly Action _onLogoutRequested;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private ViewModelBase? _currentViewModel;

        [ObservableProperty]
        private string _currentSectionTitle = "NGEOS Humanitarian Welfare Platform";

        [ObservableProperty]
        private bool _isAdminMode = false;

        [ObservableProperty]
        private bool _isDarkTheme = true;

        [ObservableProperty]
        private bool _isOnline;

        [ObservableProperty]
        private bool _isSyncing;

        [ObservableProperty]
        private int _pendingSyncCount;

        [ObservableProperty]
        private string _syncStatusText = "Checking Connectivity...";

        [ObservableProperty]
        private string _syncButtonLabel = "Sync Now";

        // Admin ViewModels
        public DashboardViewModel DashboardVM { get; }
        public CausesViewModel CausesVM { get; }
        public DonationsViewModel DonationsVM { get; }
        public ReportsViewModel ReportsVM { get; }
        public BackupViewModel BackupVM { get; }
        public AuditLogsViewModel AuditLogsVM { get; }
        public SettingsViewModel SettingsVM { get; }

        // Website Public & Citizen ViewModels
        public HomePortalViewModel HomeVM { get; }
        public CausesCatalogViewModel CausesCatalogVM { get; }
        public CauseDetailsViewModel CauseDetailsVM { get; }
        public PublicDonateViewModel PublicDonateVM { get; }
        public ProgrammesViewModel ProgrammesVM { get; }
        public ProgrammeDetailsViewModel ProgrammeDetailsVM { get; }
        public EventsViewModel EventsVM { get; }
        public GalleryViewModel GalleryVM { get; }
        public NgosDirectoryViewModel NgosDirectoryVM { get; }
        public HelpCentreFaqViewModel HelpCentreFaqVM { get; }
        public UserPortalViewModel UserPortalVM { get; }
        public ZakatCalculatorViewModel ZakatVM { get; }
        public AboutViewModel AboutVM { get; }
        public ContactViewModel ContactVM { get; }

        public MainViewModel(
            IAuthService authService,
            IServiceProvider serviceProvider,
            IConnectivityService connectivity,
            ISyncService syncService,
            DashboardViewModel dashboardVM,
            CausesViewModel causesVM,
            DonationsViewModel donationsVM,
            ReportsViewModel reportsVM,
            BackupViewModel backupVM,
            AuditLogsViewModel auditLogsVM,
            SettingsViewModel settingsVM,
            HomePortalViewModel homeVM,
            CausesCatalogViewModel causesCatalogVM,
            CauseDetailsViewModel causeDetailsVM,
            PublicDonateViewModel publicDonateVM,
            ProgrammesViewModel programmesVM,
            ProgrammeDetailsViewModel programmeDetailsVM,
            EventsViewModel eventsVM,
            GalleryViewModel galleryVM,
            NgosDirectoryViewModel ngosDirectoryVM,
            HelpCentreFaqViewModel helpCentreFaqVM,
            UserPortalViewModel userPortalVM,
            ZakatCalculatorViewModel zakatVM,
            AboutViewModel aboutVM,
            ContactViewModel contactVM,
            Action onLogoutRequested)
        {
            _authService = authService;
            _serviceProvider = serviceProvider;
            _connectivity = connectivity;
            _syncService = syncService;

            DashboardVM = dashboardVM;
            CausesVM = causesVM;
            DonationsVM = donationsVM;
            ReportsVM = reportsVM;
            BackupVM = backupVM;
            AuditLogsVM = auditLogsVM;
            SettingsVM = settingsVM;

            HomeVM = homeVM;
            CausesCatalogVM = causesCatalogVM;
            CauseDetailsVM = causeDetailsVM;
            PublicDonateVM = publicDonateVM;
            ProgrammesVM = programmesVM;
            ProgrammeDetailsVM = programmeDetailsVM;
            EventsVM = eventsVM;
            GalleryVM = galleryVM;
            NgosDirectoryVM = ngosDirectoryVM;
            HelpCentreFaqVM = helpCentreFaqVM;
            UserPortalVM = userPortalVM;
            ZakatVM = zakatVM;
            AboutVM = aboutVM;
            ContactVM = contactVM;

            _onLogoutRequested = onLogoutRequested;

            // Default to Website Home Page!
            IsAdminMode = false;
            CurrentViewModel = HomeVM;
            CurrentSectionTitle = "NGEOS Humanitarian Welfare Platform";

            // Connectivity & Sync listeners
            _connectivity.ConnectivityChanged += OnConnectivityChanged;
            _syncService.SyncStateChanged += OnSyncStateChanged;

            UpdateConnectivityStatus(_connectivity.IsOnline);
            UpdateSyncState();
        }

        private void OnConnectivityChanged(bool online)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                UpdateConnectivityStatus(online);
            });
        }

        private void OnSyncStateChanged()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                UpdateSyncState();
            });
        }

        private void UpdateConnectivityStatus(bool online)
        {
            IsOnline = online;
            SyncStatusText = online 
                ? "ONLINE (Remote Cloud Connected)" 
                : "OFFLINE (Local SQLite Safe Mode)";
        }

        private void UpdateSyncState()
        {
            IsSyncing = _syncService.IsSyncing;
            PendingSyncCount = _syncService.PendingCount;
            SyncButtonLabel = IsSyncing 
                ? "Syncing..." 
                : (PendingSyncCount > 0 ? $"Sync Now ({PendingSyncCount})" : "Sync Now");
        }

        public async Task InitializeUserAsync(User user)
        {
            CurrentUser = user;
            await HomeVM.LoadAsync();
            await _connectivity.CheckConnectivityAsync();
            PendingSyncCount = await _syncService.GetPendingCountAsync();
            UpdateSyncState();
        }

        public async Task HandlePortalNavigationAsync(string target, object? parameter)
        {
            switch (target)
            {
                case "Donate":
                    await NavigateToDonateAsync(parameter as Cause);
                    break;
                case "DonateWithZakat":
                    await NavigateToDonateAsync(null);
                    if (parameter is decimal zakatAmt)
                    {
                        PublicDonateVM.Amount = zakatAmt;
                        PublicDonateVM.CustomAmountText = zakatAmt.ToString("F0");
                        PublicDonateVM.DonationType = "Zakat Fund";
                    }
                    break;
                case "Causes":
                    await NavigateToCausesCatalogAsync();
                    break;
                case "CauseDetails":
                    if (parameter is Cause cause)
                    {
                        await NavigateToCauseDetailsAsync(cause);
                    }
                    break;
                case "Programmes":
                    await NavigateToProgrammesAsync();
                    break;
                case "ProgrammeDetails":
                    if (parameter is Programme programme)
                    {
                        NavigateToProgrammeDetails(programme);
                    }
                    break;
                case "Events":
                    await NavigateToEventsAsync();
                    break;
                case "Gallery":
                    await NavigateToGalleryAsync();
                    break;
                case "Ngos":
                    await NavigateToNgosAsync();
                    break;
                case "HelpCentre":
                    await NavigateToHelpCentreAsync();
                    break;
                case "UserPortal":
                    await NavigateToUserPortalAsync();
                    break;
                case "Zakat":
                    NavigateToZakat();
                    break;
                case "About":
                    await NavigateToAboutAsync();
                    break;
                case "Contact":
                    NavigateToContact();
                    break;
                default:
                    await NavigateToHomeAsync();
                    break;
            }
        }

        [RelayCommand]
        public async Task SynchronizeNowAsync()
        {
            if (IsSyncing) return;

            var res = await _syncService.SynchronizeNowAsync();
            if (res.Success)
            {
                MessageBox.Show(res.Message, "Hybrid Synchronization", MessageBoxButton.OK, MessageBoxImage.Information);
                if (CurrentViewModel == HomeVM) await HomeVM.LoadAsync();
                else if (CurrentViewModel == CausesCatalogVM) await CausesCatalogVM.LoadAsync();
                else if (CurrentViewModel == DashboardVM) await DashboardVM.LoadAsync();
                else if (CurrentViewModel == CausesVM) await CausesVM.LoadAsync();
                else if (CurrentViewModel == DonationsVM) await DonationsVM.LoadAsync();
            }
            else
            {
                MessageBox.Show(res.Message, "Offline Status", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ==========================================
        // WEBSITE PORTAL NAVIGATION COMMANDS
        // ==========================================
        [RelayCommand]
        public async Task NavigateToHomeAsync()
        {
            IsAdminMode = false;
            CurrentSectionTitle = "NGEOS Humanitarian Welfare Platform - Home";
            CurrentViewModel = HomeVM;
            await HomeVM.LoadAsync();
        }

        [RelayCommand]
        public async Task NavigateToCausesCatalogAsync()
        {
            IsAdminMode = false;
            CurrentSectionTitle = "Humanitarian Causes & Appeals";
            CurrentViewModel = CausesCatalogVM;
            await CausesCatalogVM.LoadAsync();
        }

        [RelayCommand]
        public async Task NavigateToCauseDetailsAsync(Cause cause)
        {
            IsAdminMode = false;
            CurrentSectionTitle = $"Cause: {cause.Title}";
            CurrentViewModel = CauseDetailsVM;
            await CauseDetailsVM.LoadAsync(cause);
        }

        [RelayCommand]
        public async Task NavigateToDonateAsync(Cause? cause = null)
        {
            CurrentSectionTitle = "Make A Contribution - NGEOS Portal";
            CurrentViewModel = PublicDonateVM;
            await PublicDonateVM.LoadAsync(cause);
        }

        [RelayCommand]
        public async Task NavigateToProgrammesAsync()
        {
            IsAdminMode = false;
            CurrentSectionTitle = "Humanitarian Programmes & Field Missions";
            CurrentViewModel = ProgrammesVM;
            await ProgrammesVM.LoadAsync();
        }

        [RelayCommand]
        public void NavigateToProgrammeDetails(Programme programme)
        {
            IsAdminMode = false;
            CurrentSectionTitle = $"Mission: {programme.Title}";
            CurrentViewModel = ProgrammeDetailsVM;
            ProgrammeDetailsVM.Load(programme);
        }

        [RelayCommand]
        public async Task NavigateToEventsAsync()
        {
            IsAdminMode = false;
            CurrentSectionTitle = "Humanitarian Events & Volunteer Drives";
            CurrentViewModel = EventsVM;
            await EventsVM.LoadAsync();
        }

        [RelayCommand]
        public async Task NavigateToGalleryAsync()
        {
            IsAdminMode = false;
            CurrentSectionTitle = "Field Impact Photo Gallery";
            CurrentViewModel = GalleryVM;
            await GalleryVM.LoadAsync();
        }

        [RelayCommand]
        public async Task NavigateToNgosAsync()
        {
            IsAdminMode = false;
            CurrentSectionTitle = "Accredited & Certified NGO Partners";
            CurrentViewModel = NgosDirectoryVM;
            await NgosDirectoryVM.LoadAsync();
        }

        [RelayCommand]
        public async Task NavigateToHelpCentreAsync()
        {
            IsAdminMode = false;
            CurrentSectionTitle = "Help Centre & Frequently Asked Questions";
            CurrentViewModel = HelpCentreFaqVM;
            await HelpCentreFaqVM.LoadAsync();
        }

        [RelayCommand]
        public async Task NavigateToUserPortalAsync()
        {
            if (CurrentUser == null) return;
            IsAdminMode = false;
            CurrentSectionTitle = $"Citizen Donor Portal - {CurrentUser.FullName}";
            CurrentViewModel = UserPortalVM;
            await UserPortalVM.LoadAsync(CurrentUser);
        }

        [RelayCommand]
        public void NavigateToZakat()
        {
            CurrentSectionTitle = "Interactive Zakat Calculator - 100% Shariah Audited";
            CurrentViewModel = ZakatVM;
        }

        [RelayCommand]
        public async Task NavigateToAboutAsync()
        {
            CurrentSectionTitle = "About NGEOS Humanitarian Alliance";
            CurrentViewModel = AboutVM;
            await AboutVM.LoadAsync();
        }

        [RelayCommand]
        public void NavigateToContact()
        {
            CurrentSectionTitle = "24/7 Global Helpdesk & Humanitarian Inquiries";
            CurrentViewModel = ContactVM;
        }

        // ==========================================
        // ADMIN MANAGEMENT PORTAL COMMANDS
        // ==========================================
        [RelayCommand]
        public async Task SwitchToAdminPortalAsync()
        {
            IsAdminMode = true;
            await NavigateToDashboardAsync();
        }

        [RelayCommand]
        public async Task SwitchToWebsitePortalAsync()
        {
            IsAdminMode = false;
            await NavigateToHomeAsync();
        }

        [RelayCommand]
        public async Task NavigateToDashboardAsync()
        {
            CurrentSectionTitle = "Executive Management Dashboard";
            CurrentViewModel = DashboardVM;
            await DashboardVM.LoadAsync();
        }

        [RelayCommand]
        public async Task NavigateToCausesAsync()
        {
            CurrentSectionTitle = "Causes Management";
            CurrentViewModel = CausesVM;
            await CausesVM.LoadAsync();
        }

        [RelayCommand]
        public async Task NavigateToDonationsAsync()
        {
            CurrentSectionTitle = "Donations & Financial Ledger";
            CurrentViewModel = DonationsVM;
            await DonationsVM.LoadAsync();
        }

        [RelayCommand]
        public void NavigateToReports()
        {
            CurrentSectionTitle = "Certified Reports & Analytics";
            CurrentViewModel = ReportsVM;
        }

        [RelayCommand]
        public async Task NavigateToBackupAsync()
        {
            CurrentSectionTitle = "Database Backup & Safe Restore";
            CurrentViewModel = BackupVM;
            await BackupVM.LoadAsync();
        }

        [RelayCommand]
        public async Task NavigateToAuditLogsAsync()
        {
            CurrentSectionTitle = "Security Audit Logs & Compliance";
            CurrentViewModel = AuditLogsVM;
            await AuditLogsVM.LoadAsync();
        }

        [RelayCommand]
        public void NavigateToSettings()
        {
            CurrentSectionTitle = "System Configuration & Diagnostics";
            CurrentViewModel = SettingsVM;
        }

        [RelayCommand]
        public void Logout()
        {
            _authService.Logout();
            _onLogoutRequested();
        }
    }
}
