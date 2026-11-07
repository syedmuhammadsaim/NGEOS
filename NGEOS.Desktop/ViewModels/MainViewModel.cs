using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using NGEOS.Desktop.Models;
using NGEOS.Desktop.Security;
using NGEOS.Desktop.Services;

namespace NGEOS.Desktop.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly ISyncService _syncService;
        private readonly IRealtimeService _realtimeService;
        private readonly ISecureStorageService _secureStorage;
        private readonly IReportService _reportService;
        private readonly Action _onLogout;

        private LocalUser _currentUser;
        private ViewModelBase _currentView;
        private string _activeTab = "Dashboard";
        private bool _isOnline;
        private string _lastSyncDisplay = "Never";
        private int _pendingSyncCount;
        private bool _isDarkTheme;

        public LocalUser CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string ActiveTab
        {
            get => _activeTab;
            set => SetProperty(ref _activeTab, value);
        }

        public bool IsOnline
        {
            get => _isOnline;
            set => SetProperty(ref _isOnline, value);
        }

        public string LastSyncDisplay
        {
            get => _lastSyncDisplay;
            set => SetProperty(ref _lastSyncDisplay, value);
        }

        public int PendingSyncCount
        {
            get => _pendingSyncCount;
            set => SetProperty(ref _pendingSyncCount, value);
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (SetProperty(ref _isDarkTheme, value))
                {
                    ApplyTheme(value);
                }
            }
        }

        public bool IsAdmin => CurrentUser?.Role == "Admin";

        public ObservableCollection<string> RealtimeNotifications { get; } = new();

        // Navigation Commands
        public IRelayCommand NavigateDashboardCommand { get; }
        public IRelayCommand NavigateDonationsCommand { get; }
        public IRelayCommand NavigateCampaignsCommand { get; }
        public IRelayCommand NavigateProjectsCommand { get; }
        public IRelayCommand NavigateQueriesCommand { get; }
        public IRelayCommand NavigateReportsCommand { get; }
        public IRelayCommand NavigateSettingsCommand { get; }

        public IAsyncRelayCommand TriggerManualSyncCommand { get; }
        public IRelayCommand ToggleThemeCommand { get; }
        public IRelayCommand LogoutCommand { get; }

        public MainViewModel(
            LocalUser user,
            IApiService apiService,
            ISyncService syncService,
            IRealtimeService realtimeService,
            ISecureStorageService secureStorage,
            IReportService reportService,
            Action onLogout)
        {
            _currentUser = user;
            _apiService = apiService;
            _syncService = syncService;
            _realtimeService = realtimeService;
            _secureStorage = secureStorage;
            _reportService = reportService;
            _onLogout = onLogout;

            NavigateDashboardCommand = new RelayCommand(NavigateDashboard);
            NavigateDonationsCommand = new RelayCommand(NavigateDonations);
            NavigateCampaignsCommand = new RelayCommand(NavigateCampaigns);
            NavigateProjectsCommand = new RelayCommand(NavigateProjects);
            NavigateQueriesCommand = new RelayCommand(NavigateQueries);
            NavigateReportsCommand = new RelayCommand(NavigateReports);
            NavigateSettingsCommand = new RelayCommand(NavigateSettings);

            TriggerManualSyncCommand = new AsyncRelayCommand(ExecuteManualSyncAsync);
            ToggleThemeCommand = new RelayCommand(() => IsDarkTheme = !IsDarkTheme);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            IsOnline = _apiService.IsOnline;
            UpdateLastSync();

            // Wire sync and connection events
            _apiService.ConnectivityChanged += (_, online) =>
            {
                Application.Current.Dispatcher.Invoke(() => IsOnline = online);
            };

            _syncService.PendingCountChanged += (_, count) =>
            {
                Application.Current.Dispatcher.Invoke(() => PendingSyncCount = count);
            };

            _syncService.SyncCompleted += (_, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateLastSync();
                    if (!string.IsNullOrEmpty(e.Message))
                    {
                        RealtimeNotifications.Insert(0, $"[Sync] {e.Message}");
                    }
                });
            };

            _realtimeService.RealtimeNotificationReceived += (_, msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RealtimeNotifications.Insert(0, msg);
                });
            };

            // Start SignalR
            _ = _realtimeService.StartAsync();

            // Default to Dashboard
            NavigateDashboard();
        }

        private void UpdateLastSync()
        {
            if (_syncService.LastSyncTime.HasValue)
            {
                LastSyncDisplay = _syncService.LastSyncTime.Value.ToString("MMM dd, HH:mm");
            }
        }

        public void NavigateDashboard()
        {
            ActiveTab = "Dashboard";
            if (IsAdmin)
            {
                CurrentView = new AdminDashboardViewModel(_apiService, _syncService);
            }
            else
            {
                CurrentView = new UserDashboardViewModel(CurrentUser, _apiService, _syncService);
            }
            _ = CurrentView.InitializeAsync();
        }

        public void NavigateDonations()
        {
            ActiveTab = "Donations";
            CurrentView = new DonationsViewModel(CurrentUser, _apiService, _reportService);
            _ = CurrentView.InitializeAsync();
        }

        public void NavigateCampaigns()
        {
            ActiveTab = "Campaigns";
            CurrentView = new CampaignsViewModel(CurrentUser, _apiService, _syncService);
            _ = CurrentView.InitializeAsync();
        }

        public void NavigateProjects()
        {
            ActiveTab = "Projects";
            CurrentView = new ProjectsViewModel(CurrentUser, _apiService, _syncService);
            _ = CurrentView.InitializeAsync();
        }

        public void NavigateQueries()
        {
            ActiveTab = "Queries";
            CurrentView = new QueriesViewModel(CurrentUser, _apiService, _syncService);
            _ = CurrentView.InitializeAsync();
        }

        public void NavigateReports()
        {
            ActiveTab = "Reports";
            CurrentView = new ReportsViewModel(CurrentUser, _apiService, _reportService);
            _ = CurrentView.InitializeAsync();
        }

        public void NavigateSettings()
        {
            ActiveTab = "Settings";
            CurrentView = new SettingsViewModel(CurrentUser, _apiService, _secureStorage, _syncService);
            _ = CurrentView.InitializeAsync();
        }

        private async Task ExecuteManualSyncAsync()
        {
            IsBusy = true;
            StatusMessage = "Synchronizing...";
            try
            {
                var summary = await _syncService.TriggerSyncAsync();
                UpdateLastSync();
                StatusMessage = summary.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteLogout()
        {
            _secureStorage.ClearToken();
            _apiService.SetAuthToken(null);
            _ = _realtimeService.StopAsync();
            _onLogout();
        }

        private void ApplyTheme(bool dark)
        {
            // Changes resources dynamically for Dark / Light mode
            var app = Application.Current;
            if (dark)
            {
                app.Resources["AppBgBrush"] = app.Resources["DarkAppBgBrush"];
                app.Resources["CardBgBrush"] = app.Resources["DarkCardBgBrush"];
                app.Resources["TextPrimaryBrush"] = app.Resources["DarkTextPrimaryBrush"];
                app.Resources["TextSecondaryBrush"] = app.Resources["DarkTextSecondaryBrush"];
                app.Resources["BorderBrush"] = app.Resources["DarkBorderBrush"];
            }
            else
            {
                app.Resources["AppBgBrush"] = app.Resources["LightAppBgBrush"];
                app.Resources["CardBgBrush"] = app.Resources["LightCardBgBrush"];
                app.Resources["TextPrimaryBrush"] = app.Resources["LightTextPrimaryBrush"];
                app.Resources["TextSecondaryBrush"] = app.Resources["LightTextSecondaryBrush"];
                app.Resources["BorderBrush"] = app.Resources["LightBorderBrush"];
            }
        }
    }
}
