using System;
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
        private readonly Action _onLogoutRequested;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private ViewModelBase? _currentViewModel;

        [ObservableProperty]
        private string _currentSectionTitle = "Dashboard Overview";

        [ObservableProperty]
        private bool _isDarkTheme = true;

        // ViewModels
        public DashboardViewModel DashboardVM { get; }
        public CausesViewModel CausesVM { get; }
        public DonationsViewModel DonationsVM { get; }
        public ReportsViewModel ReportsVM { get; }
        public BackupViewModel BackupVM { get; }
        public AuditLogsViewModel AuditLogsVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public MainViewModel(
            IAuthService authService,
            IServiceProvider serviceProvider,
            DashboardViewModel dashboardVM,
            CausesViewModel causesVM,
            DonationsViewModel donationsVM,
            ReportsViewModel reportsVM,
            BackupViewModel backupVM,
            AuditLogsViewModel auditLogsVM,
            SettingsViewModel settingsVM,
            Action onLogoutRequested)
        {
            _authService = authService;
            _serviceProvider = serviceProvider;
            DashboardVM = dashboardVM;
            CausesVM = causesVM;
            DonationsVM = donationsVM;
            ReportsVM = reportsVM;
            BackupVM = backupVM;
            AuditLogsVM = auditLogsVM;
            SettingsVM = settingsVM;
            _onLogoutRequested = onLogoutRequested;

            // Default view
            CurrentViewModel = DashboardVM;
            CurrentSectionTitle = "Executive Dashboard";
        }

        public async Task InitializeUserAsync(User user)
        {
            CurrentUser = user;
            await DashboardVM.LoadAsync();
        }

        [RelayCommand]
        public async Task NavigateToDashboardAsync()
        {
            CurrentSectionTitle = "Executive Dashboard";
            CurrentViewModel = DashboardVM;
            await DashboardVM.LoadAsync();
        }

        [RelayCommand]
        public async Task NavigateToCausesAsync()
        {
            CurrentSectionTitle = "Humanitarian Causes & Campaigns";
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
        public void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
            ApplyTheme(IsDarkTheme);
        }

        private static void ApplyTheme(bool isDark)
        {
            var res = Application.Current.Resources;
            if (isDark)
            {
                // Dark Slate Navy
                res["AppBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B132B"));
                res["CardBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C2541"));
                res["SidebarBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B192C"));
                res["TopBarBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D1B2A"));
                res["TextPrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
                res["TextSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                res["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
                res["InputBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
                res["DataGridHeaderBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
                res["DataGridRowAlternateBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#162238"));
                res["DataGridRowHoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24344D"));
            }
            else
            {
                // Clean Professional Light
                res["AppBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                res["CardBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                res["SidebarBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")); // Keep sleek dark sidebar or clean slate
                res["TopBarBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                res["TextPrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
                res["TextSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                res["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                res["InputBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
                res["DataGridHeaderBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
                res["DataGridRowAlternateBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
                res["DataGridRowHoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDF2F7"));
            }
        }

        [RelayCommand]
        public void Logout()
        {
            var res = MessageBox.Show("Are you sure you want to log out of NGEOS Desktop?", "Confirm Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                _authService.Logout();
                _onLogoutRequested();
            }
        }
    }
}
