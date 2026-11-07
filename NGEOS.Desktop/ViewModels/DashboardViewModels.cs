using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NGEOS.Desktop.Data;
using NGEOS.Desktop.Models;
using NGEOS.Desktop.Services;

namespace NGEOS.Desktop.ViewModels
{
    public class AdminDashboardViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly ISyncService _syncService;

        private decimal _totalDonations;
        private int _totalDonors;
        private int _totalCampaigns;
        private int _totalBeneficiaries;
        private int _totalProjects;
        private int _pendingRequests;
        private int _approvedRequests;
        private int _rejectedRequests;

        public decimal TotalDonations { get => _totalDonations; set => SetProperty(ref _totalDonations, value); }
        public int TotalDonors { get => _totalDonors; set => SetProperty(ref _totalDonors, value); }
        public int TotalCampaigns { get => _totalCampaigns; set => SetProperty(ref _totalCampaigns, value); }
        public int TotalBeneficiaries { get => _totalBeneficiaries; set => SetProperty(ref _totalBeneficiaries, value); }
        public int TotalProjects { get => _totalProjects; set => SetProperty(ref _totalProjects, value); }
        public int PendingRequests { get => _pendingRequests; set => SetProperty(ref _pendingRequests, value); }
        public int ApprovedRequests { get => _approvedRequests; set => SetProperty(ref _approvedRequests, value); }
        public int RejectedRequests { get => _rejectedRequests; set => SetProperty(ref _rejectedRequests, value); }

        public ObservableCollection<LocalDonation> RecentTransactions { get; } = new();
        public ObservableCollection<ChartItem> MonthlyChart { get; } = new();
        public ObservableCollection<CategoryStatItem> CategoryStats { get; } = new();
        public ObservableCollection<ActivityItem> RecentActivities { get; } = new();

        public IAsyncRelayCommand RefreshCommand { get; }

        public AdminDashboardViewModel(IApiService apiService, ISyncService syncService)
        {
            _apiService = apiService;
            _syncService = syncService;
            RefreshCommand = new AsyncRelayCommand(InitializeAsync);
        }

        public override async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                if (_apiService.IsOnline)
                {
                    var data = await _apiService.GetAdminDashboardAsync();
                    if (data != null)
                    {
                        TotalDonations = data.TotalDonations;
                        TotalDonors = data.TotalDonors;
                        TotalCampaigns = data.TotalCampaigns;
                        TotalBeneficiaries = data.TotalBeneficiaries;
                        TotalProjects = data.TotalProjects;
                        PendingRequests = data.PendingRequests;
                        ApprovedRequests = data.ApprovedRequests;
                        RejectedRequests = data.RejectedRequests;

                        RecentTransactions.Clear();
                        foreach (var tx in data.RecentTransactions) RecentTransactions.Add(tx);

                        MonthlyChart.Clear();
                        foreach (var ch in data.MonthlyChart) MonthlyChart.Add(ch);

                        CategoryStats.Clear();
                        foreach (var cat in data.CategoryStats) CategoryStats.Add(cat);

                        RecentActivities.Clear();
                        foreach (var act in data.RecentActivities) RecentActivities.Add(act);
                        return;
                    }
                }

                // OFFLINE FALLBACK: Calculate directly from local SQLite cache!
                using var db = new OfflineDbContext();
                await db.Database.EnsureCreatedAsync();

                var donations = await db.Donations.ToListAsync();
                TotalDonations = donations.Where(d => d.PaymentStatus == "Completed").Sum(d => d.Amount);
                TotalDonors = donations.Select(d => d.DonorEmail.ToLower()).Distinct().Count();
                TotalCampaigns = await db.Causes.CountAsync();
                TotalProjects = await db.Programmes.CountAsync();
                TotalBeneficiaries = await db.Programmes.SumAsync(p => p.TargetBeneficiaries);
                PendingRequests = await db.Queries.CountAsync(q => q.Status == "Pending");

                RecentTransactions.Clear();
                foreach (var tx in donations.OrderByDescending(d => d.DonationDate).Take(10))
                {
                    RecentTransactions.Add(tx);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class UserDashboardViewModel : ViewModelBase
    {
        private readonly LocalUser _user;
        private readonly IApiService _apiService;
        private readonly ISyncService _syncService;

        private decimal _totalDonated;
        private int _causesSupportedCount;
        private int _programmesJoinedCount;
        private int _queriesCount;

        public LocalUser User => _user;
        public decimal TotalDonated { get => _totalDonated; set => SetProperty(ref _totalDonated, value); }
        public int CausesSupportedCount { get => _causesSupportedCount; set => SetProperty(ref _causesSupportedCount, value); }
        public int ProgrammesJoinedCount { get => _programmesJoinedCount; set => SetProperty(ref _programmesJoinedCount, value); }
        public int QueriesCount { get => _queriesCount; set => SetProperty(ref _queriesCount, value); }

        public ObservableCollection<LocalDonation> RecentDonations { get; } = new();
        public ObservableCollection<LocalQuery> MyQueries { get; } = new();

        public IAsyncRelayCommand RefreshCommand { get; }

        public UserDashboardViewModel(LocalUser user, IApiService apiService, ISyncService syncService)
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
                if (_apiService.IsOnline)
                {
                    var data = await _apiService.GetUserDashboardAsync();
                    if (data != null)
                    {
                        TotalDonated = data.TotalDonated;
                        CausesSupportedCount = data.CausesSupportedCount;
                        ProgrammesJoinedCount = data.ProgrammesJoinedCount;
                        QueriesCount = data.QueriesCount;

                        RecentDonations.Clear();
                        foreach (var d in data.RecentDonations) RecentDonations.Add(d);

                        MyQueries.Clear();
                        foreach (var q in data.MyQueries) MyQueries.Add(q);
                        return;
                    }
                }

                // OFFLINE FALLBACK: Calculate from local SQLite
                using var db = new OfflineDbContext();
                await db.Database.EnsureCreatedAsync();

                var myEmail = _user.Email.ToLower();
                var donations = await db.Donations
                    .Where(d => d.DonorEmail.ToLower() == myEmail)
                    .OrderByDescending(d => d.DonationDate)
                    .ToListAsync();

                TotalDonated = donations.Where(d => d.PaymentStatus == "Completed").Sum(d => d.Amount);
                CausesSupportedCount = donations.Select(d => d.CauseTitle).Distinct().Count();
                QueriesCount = await db.Queries.CountAsync(q => q.Email.ToLower() == myEmail);

                RecentDonations.Clear();
                foreach (var d in donations.Take(5)) RecentDonations.Add(d);

                var queries = await db.Queries
                    .Where(q => q.Email.ToLower() == myEmail)
                    .OrderByDescending(q => q.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                MyQueries.Clear();
                foreach (var q in queries) MyQueries.Add(q);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
