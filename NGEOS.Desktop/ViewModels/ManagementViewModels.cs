using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NGEOS.Desktop.Data;
using NGEOS.Desktop.Models;
using NGEOS.Desktop.Security;
using NGEOS.Desktop.Services;

namespace NGEOS.Desktop.ViewModels
{
    public class DonationsViewModel : ViewModelBase
    {
        private readonly LocalUser _user;
        private readonly IApiService _apiService;
        private readonly IReportService _reportService;

        private string _searchText = string.Empty;
        private string _selectedStatus = "All";
        private decimal _filteredTotal;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    ApplyFilter();
                }
            }
        }

        public decimal FilteredTotal
        {
            get => _filteredTotal;
            set => SetProperty(ref _filteredTotal, value);
        }

        public List<LocalDonation> AllDonations { get; } = new();
        public ObservableCollection<LocalDonation> DisplayedDonations { get; } = new();

        public IAsyncRelayCommand RefreshCommand { get; }
        public IRelayCommand ExportExcelCommand { get; }
        public IRelayCommand PrintReportCommand { get; }

        public DonationsViewModel(LocalUser user, IApiService apiService, IReportService reportService)
        {
            _user = user;
            _apiService = apiService;
            _reportService = reportService;

            RefreshCommand = new AsyncRelayCommand(InitializeAsync);
            ExportExcelCommand = new RelayCommand(ExportToExcel);
            PrintReportCommand = new RelayCommand(PrintReport);
        }

        public override async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                List<LocalDonation> list;
                if (_apiService.IsOnline)
                {
                    list = await _apiService.GetDonationsAsync();
                }
                else
                {
                    using var db = new OfflineDbContext();
                    await db.Database.EnsureCreatedAsync();
                    if (_user.Role == "Admin")
                    {
                        list = await db.Donations.OrderByDescending(d => d.DonationDate).ToListAsync();
                    }
                    else
                    {
                        var myEmail = _user.Email.ToLower();
                        list = await db.Donations.Where(d => d.DonorEmail.ToLower() == myEmail).OrderByDescending(d => d.DonationDate).ToListAsync();
                    }
                }

                AllDonations.Clear();
                AllDonations.AddRange(list);
                ApplyFilter();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            var filtered = AllDonations.AsEnumerable();

            if (SelectedStatus != "All")
            {
                filtered = filtered.Where(d => d.PaymentStatus.Equals(SelectedStatus, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim().ToLower();
                filtered = filtered.Where(d =>
                    d.DonorName.ToLower().Contains(term) ||
                    d.TransactionId.ToLower().Contains(term) ||
                    d.CauseTitle.ToLower().Contains(term) ||
                    d.PaymentGateway.ToLower().Contains(term));
            }

            DisplayedDonations.Clear();
            decimal sum = 0;
            foreach (var d in filtered)
            {
                DisplayedDonations.Add(d);
                sum += d.Amount;
            }
            FilteredTotal = sum;
        }

        private void ExportToExcel()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"NGEOS_Donations_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                _reportService.ExportDonationsToExcel(DisplayedDonations.ToList(), sfd.FileName);
                StatusMessage = $"Saved: {sfd.FileName}";
                Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
            }
        }

        private void PrintReport()
        {
            var path = _reportService.GenerateHtmlReport(DisplayedDonations.ToList(), "Donation Audit & Tax Report");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }

    public class CampaignsViewModel : ViewModelBase
    {
        private readonly LocalUser _user;
        private readonly IApiService _apiService;
        private readonly ISyncService _syncService;

        private string _newTitle = string.Empty;
        private string _newCategory = "Education";
        private decimal _newTargetAmount = 500000;
        private string _newDescription = string.Empty;
        private bool _isCreatePopupOpen;

        public bool IsAdmin => _user.Role == "Admin";

        public string NewTitle { get => _newTitle; set => SetProperty(ref _newTitle, value); }
        public string NewCategory { get => _newCategory; set => SetProperty(ref _newCategory, value); }
        public decimal NewTargetAmount { get => _newTargetAmount; set => SetProperty(ref _newTargetAmount, value); }
        public string NewDescription { get => _newDescription; set => SetProperty(ref _newDescription, value); }
        public bool IsCreatePopupOpen { get => _isCreatePopupOpen; set => SetProperty(ref _isCreatePopupOpen, value); }

        public ObservableCollection<LocalCause> Causes { get; } = new();

        public IAsyncRelayCommand RefreshCommand { get; }
        public IRelayCommand OpenCreateDialogCommand { get; }
        public IRelayCommand CloseCreateDialogCommand { get; }
        public IAsyncRelayCommand SaveCampaignCommand { get; }

        public CampaignsViewModel(LocalUser user, IApiService apiService, ISyncService syncService)
        {
            _user = user;
            _apiService = apiService;
            _syncService = syncService;

            RefreshCommand = new AsyncRelayCommand(InitializeAsync);
            OpenCreateDialogCommand = new RelayCommand(() => IsCreatePopupOpen = true);
            CloseCreateDialogCommand = new RelayCommand(() => IsCreatePopupOpen = false);
            SaveCampaignCommand = new AsyncRelayCommand(ExecuteSaveCampaignAsync);
        }

        public override async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                List<LocalCause> list;
                if (_apiService.IsOnline)
                {
                    list = await _apiService.GetCausesAsync();
                }
                else
                {
                    using var db = new OfflineDbContext();
                    await db.Database.EnsureCreatedAsync();
                    list = await db.Causes.Where(c => c.IsActive).OrderByDescending(c => c.CreatedAt).ToListAsync();
                }

                Causes.Clear();
                foreach (var c in list) Causes.Add(c);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteSaveCampaignAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTitle)) return;

            var cause = new LocalCause
            {
                Title = NewTitle,
                Category = NewCategory,
                TargetAmount = NewTargetAmount,
                ShortDescription = NewDescription,
                FullDescription = NewDescription,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            if (_apiService.IsOnline)
            {
                await _apiService.CreateCauseAsync(cause);
            }
            else
            {
                // Queue for offline sync
                await _syncService.QueueOperationAsync("Cause", "Create", cause);

                // Add to local DB immediately
                using var db = new OfflineDbContext();
                await db.Database.EnsureCreatedAsync();
                cause.IsSynced = false;
                db.Causes.Add(cause);
                await db.SaveChangesAsync();
            }

            IsCreatePopupOpen = false;
            NewTitle = string.Empty;
            NewDescription = string.Empty;
            await InitializeAsync();
        }
    }

    public class ProjectsViewModel : ViewModelBase
    {
        private readonly LocalUser _user;
        private readonly IApiService _apiService;
        private readonly ISyncService _syncService;

        public bool IsAdmin => _user.Role == "Admin";
        public ObservableCollection<LocalProgramme> Programmes { get; } = new();

        public IAsyncRelayCommand RefreshCommand { get; }

        public ProjectsViewModel(LocalUser user, IApiService apiService, ISyncService syncService)
        {
            _user = user;
            _apiService = apiService;
            _syncService = syncService;
            RefreshCommand = new AsyncRelayCommand(InitializeAsync);
        }

        public override async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                List<LocalProgramme> list;
                if (_apiService.IsOnline)
                {
                    list = await _apiService.GetProgrammesAsync();
                }
                else
                {
                    using var db = new OfflineDbContext();
                    await db.Database.EnsureCreatedAsync();
                    list = await db.Programmes.Where(p => p.IsActive).ToListAsync();
                }

                Programmes.Clear();
                foreach (var p in list) Programmes.Add(p);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class QueriesViewModel : ViewModelBase
    {
        private readonly LocalUser _user;
        private readonly IApiService _apiService;
        private readonly ISyncService _syncService;

        private string _newSubject = string.Empty;
        private string _newCategory = "General Inquiry";
        private string _newMessage = string.Empty;
        private bool _isSubmitDialogOpen;

        public bool IsAdmin => _user.Role == "Admin";
        public string NewSubject { get => _newSubject; set => SetProperty(ref _newSubject, value); }
        public string NewCategory { get => _newCategory; set => SetProperty(ref _newCategory, value); }
        public string NewMessage { get => _newMessage; set => SetProperty(ref _newMessage, value); }
        public bool IsSubmitDialogOpen { get => _isSubmitDialogOpen; set => SetProperty(ref _isSubmitDialogOpen, value); }

        public ObservableCollection<LocalQuery> Queries { get; } = new();

        public IAsyncRelayCommand RefreshCommand { get; }
        public IRelayCommand OpenSubmitDialogCommand { get; }
        public IRelayCommand CloseSubmitDialogCommand { get; }
        public IAsyncRelayCommand SubmitQueryCommand { get; }

        public QueriesViewModel(LocalUser user, IApiService apiService, ISyncService syncService)
        {
            _user = user;
            _apiService = apiService;
            _syncService = syncService;

            RefreshCommand = new AsyncRelayCommand(InitializeAsync);
            OpenSubmitDialogCommand = new RelayCommand(() => IsSubmitDialogOpen = true);
            CloseSubmitDialogCommand = new RelayCommand(() => IsSubmitDialogOpen = false);
            SubmitQueryCommand = new AsyncRelayCommand(ExecuteSubmitQueryAsync);
        }

        public override async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                List<LocalQuery> list;
                if (_apiService.IsOnline)
                {
                    list = await _apiService.GetQueriesAsync();
                }
                else
                {
                    using var db = new OfflineDbContext();
                    await db.Database.EnsureCreatedAsync();
                    if (_user.Role == "Admin")
                    {
                        list = await db.Queries.OrderByDescending(q => q.CreatedAt).ToListAsync();
                    }
                    else
                    {
                        var myEmail = _user.Email.ToLower();
                        list = await db.Queries.Where(q => q.Email.ToLower() == myEmail).OrderByDescending(q => q.CreatedAt).ToListAsync();
                    }
                }

                Queries.Clear();
                foreach (var q in list) Queries.Add(q);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteSubmitQueryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewSubject) || string.IsNullOrWhiteSpace(NewMessage)) return;

            var q = new LocalQuery
            {
                UserId = _user.Id,
                FullName = _user.FullName,
                Email = _user.Email,
                Subject = NewSubject,
                Category = NewCategory,
                Message = NewMessage,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            if (_apiService.IsOnline)
            {
                await _apiService.CreateQueryAsync(q);
            }
            else
            {
                await _syncService.QueueOperationAsync("Query", "Create", q);

                using var db = new OfflineDbContext();
                await db.Database.EnsureCreatedAsync();
                q.IsSynced = false;
                db.Queries.Add(q);
                await db.SaveChangesAsync();
            }

            IsSubmitDialogOpen = false;
            NewSubject = string.Empty;
            NewMessage = string.Empty;
            await InitializeAsync();
        }
    }

    public class ReportsViewModel : ViewModelBase
    {
        private readonly LocalUser _user;
        private readonly IApiService _apiService;
        private readonly IReportService _reportService;

        public bool IsAdmin => _user.Role == "Admin";
        public ObservableCollection<LocalDonation> ReportDonations { get; } = new();

        public IAsyncRelayCommand GenerateReportCommand { get; }
        public IRelayCommand ExportExcelCommand { get; }
        public IRelayCommand PrintCommand { get; }

        public ReportsViewModel(LocalUser user, IApiService apiService, IReportService reportService)
        {
            _user = user;
            _apiService = apiService;
            _reportService = reportService;

            GenerateReportCommand = new AsyncRelayCommand(InitializeAsync);
            ExportExcelCommand = new RelayCommand(ExportToExcel);
            PrintCommand = new RelayCommand(PrintReport);
        }

        public override async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                var list = await _apiService.GetDonationsAsync();
                ReportDonations.Clear();
                foreach (var d in list) ReportDonations.Add(d);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExportToExcel()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"NGEOS_Audit_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            };
            if (sfd.ShowDialog() == true)
            {
                _reportService.ExportDonationsToExcel(ReportDonations.ToList(), sfd.FileName);
                Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
            }
        }

        private void PrintReport()
        {
            var path = _reportService.GenerateHtmlReport(ReportDonations.ToList(), "Comprehensive NGO Financial & Operations Report");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }

    public class SettingsViewModel : ViewModelBase
    {
        private readonly LocalUser _user;
        private readonly IApiService _apiService;
        private readonly ISecureStorageService _secureStorage;
        private readonly ISyncService _syncService;

        private string _apiBaseUrl = string.Empty;
        private string _databasePath = string.Empty;
        private string _saveMessage = string.Empty;

        public LocalUser User => _user;
        public string ApiBaseUrl { get => _apiBaseUrl; set => SetProperty(ref _apiBaseUrl, value); }
        public string DatabasePath { get => _databasePath; set => SetProperty(ref _databasePath, value); }
        public string SaveMessage { get => _saveMessage; set => SetProperty(ref _saveMessage, value); }

        public IRelayCommand SaveSettingsCommand { get; }
        public IAsyncRelayCommand TriggerSyncCommand { get; }

        public SettingsViewModel(LocalUser user, IApiService apiService, ISecureStorageService secureStorage, ISyncService syncService)
        {
            _user = user;
            _apiService = apiService;
            _secureStorage = secureStorage;
            _syncService = syncService;

            ApiBaseUrl = _apiService.BaseUrl;
            DatabasePath = OfflineDbContext.GetDatabasePath();

            SaveSettingsCommand = new RelayCommand(SaveSettings);
            TriggerSyncCommand = new AsyncRelayCommand(async () => await _syncService.TriggerSyncAsync());
        }

        private void SaveSettings()
        {
            if (!string.IsNullOrWhiteSpace(ApiBaseUrl))
            {
                _apiService.BaseUrl = ApiBaseUrl.TrimEnd('/');
                _secureStorage.SaveSetting("api_base_url", _apiService.BaseUrl);
                SaveMessage = "API settings updated successfully!";
            }
        }
    }
}
