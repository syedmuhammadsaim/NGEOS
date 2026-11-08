using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NGEOS.Desktop.Models;
using NGEOS.Desktop.Security;

namespace NGEOS.Desktop.Services
{
    public interface IApiService
    {
        string BaseUrl { get; set; }
        bool IsOnline { get; }
        event EventHandler<bool>? ConnectivityChanged;

        Task<bool> CheckConnectivityAsync();
        void SetAuthToken(string? token);

        Task<ApiResponse<LocalUser>> LoginAsync(string email, string password);
        Task<AdminDashboardData?> GetAdminDashboardAsync();
        Task<UserDashboardData?> GetUserDashboardAsync();
        Task<List<LocalDonation>> GetDonationsAsync(string? status = null, string? search = null);
        Task<List<LocalCause>> GetCausesAsync();
        Task<bool> CreateCauseAsync(LocalCause cause);
        Task<bool> UpdateCauseAsync(LocalCause cause);
        Task<List<LocalProgramme>> GetProgrammesAsync();
        Task<bool> CreateProgrammeAsync(LocalProgramme programme);
        Task<bool> UpdateProgrammeAsync(LocalProgramme programme);
        Task<List<LocalQuery>> GetQueriesAsync(string? status = null);
        Task<bool> CreateQueryAsync(LocalQuery query);
        Task<bool> ReplyQueryAsync(int queryId, string reply);

        Task<SyncPullData?> PullSyncAsync(DateTime? since = null);
        Task<List<SyncResult>> PushSyncAsync(List<PendingOperation> operations);
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? Token { get; set; }
    }

    public class AdminDashboardData
    {
        public decimal TotalDonations { get; set; }
        public int TotalDonors { get; set; }
        public int TotalCampaigns { get; set; }
        public int TotalBeneficiaries { get; set; }
        public int TotalProjects { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public List<ChartItem> MonthlyChart { get; set; } = new();
        public List<CategoryStatItem> CategoryStats { get; set; } = new();
        public List<LocalDonation> RecentTransactions { get; set; } = new();
        public List<ActivityItem> RecentActivities { get; set; } = new();
    }

    public class ChartItem
    {
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class CategoryStatItem
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class ActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class UserDashboardData
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TotalDonated { get; set; }
        public int CausesSupportedCount { get; set; }
        public int ProgrammesJoinedCount { get; set; }
        public int QueriesCount { get; set; }
        public List<LocalDonation> RecentDonations { get; set; } = new();
        public List<LocalQuery> MyQueries { get; set; } = new();
    }

    public class SyncPullData
    {
        public DateTime ServerTimeUtc { get; set; }
        public List<LocalCause> Causes { get; set; } = new();
        public List<LocalProgramme> Programmes { get; set; } = new();
        public List<LocalDonation> Donations { get; set; } = new();
        public List<LocalQuery> Queries { get; set; } = new();
    }

    public class SyncResult
    {
        public string OperationId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ServerEntityId { get; set; }
    }

    public class ApiService : IApiService
    {
        private readonly HttpClient _http;
        private readonly ISecureStorageService _secureStorage;
        private bool _isOnline;

        public string BaseUrl { get; set; } = "https://ngeos.runasp.net";
        public bool IsOnline => _isOnline;
        public event EventHandler<bool>? ConnectivityChanged;

        public ApiService(ISecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;

            var savedUrl = _secureStorage.GetSetting("api_base_url");
            if (!string.IsNullOrWhiteSpace(savedUrl))
            {
                BaseUrl = savedUrl.TrimEnd('/');
            }

            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };

            var token = _secureStorage.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                SetAuthToken(token);
            }
        }

        public void SetAuthToken(string? token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _http.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<bool> CheckConnectivityAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _http.GetAsync($"{BaseUrl}/", cts.Token);
                var online = response.IsSuccessStatusCode;
                UpdateConnectivity(online);
                return online;
            }
            catch
            {
                UpdateConnectivity(false);
                return false;
            }
        }

        private void UpdateConnectivity(bool online)
        {
            if (_isOnline != online)
            {
                _isOnline = online;
                ConnectivityChanged?.Invoke(this, _isOnline);
            }
        }

        public async Task<ApiResponse<LocalUser>> LoginAsync(string email, string password)
        {
            try
            {
                var payload = new { Email = email, Password = password };
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/auth/login", payload);
                var contentString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    UpdateConnectivity(true);
                    if (string.IsNullOrWhiteSpace(contentString))
                    {
                        return new ApiResponse<LocalUser> { Success = false, Message = "Server returned empty response." };
                    }

                    using var doc = JsonDocument.Parse(contentString);
                    var root = doc.RootElement;
                    var token = root.TryGetProperty("token", out var t) ? t.GetString() ?? "" : "";
                    SetAuthToken(token);
                    _secureStorage.SaveToken(token);

                    var user = new LocalUser
                    {
                        Id = root.TryGetProperty("userId", out var uid) ? uid.GetInt32() : 1,
                        FullName = root.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? "User" : "User",
                        Email = root.TryGetProperty("email", out var em) ? em.GetString() ?? email : email,
                        Role = root.TryGetProperty("role", out var rl) ? rl.GetString() ?? "User" : "User",
                        ProfileImageUrl = root.TryGetProperty("profileImageUrl", out var pic) ? pic.GetString() : null,
                        Phone = root.TryGetProperty("phone", out var ph) ? ph.GetString() : null,
                        Address = root.TryGetProperty("address", out var ad) ? ad.GetString() : null,
                        Profession = root.TryGetProperty("profession", out var pr) ? pr.GetString() : null,
                        Organization = root.TryGetProperty("organization", out var org) ? org.GetString() : null,
                        LastLoginAt = DateTime.UtcNow
                    };

                    return new ApiResponse<LocalUser>
                    {
                        Success = true,
                        Message = root.TryGetProperty("message", out var msg) ? msg.GetString() ?? "Login successful" : "Login successful",
                        Data = user,
                        Token = token
                    };
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new ApiResponse<LocalUser> { Success = false, Message = "API v1 not yet deployed on server. Switching to secure offline mode." };
                }

                if (!string.IsNullOrWhiteSpace(contentString))
                {
                    try
                    {
                        using var errDoc = JsonDocument.Parse(contentString);
                        var errMsg = errDoc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : null;
                        return new ApiResponse<LocalUser> { Success = false, Message = errMsg ?? $"Server error: {(int)response.StatusCode}" };
                    }
                    catch { }
                }

                return new ApiResponse<LocalUser> { Success = false, Message = $"Server returned {(int)response.StatusCode} {response.ReasonPhrase}" };
            }
            catch (Exception ex)
            {
                UpdateConnectivity(false);
                return new ApiResponse<LocalUser> { Success = false, Message = $"Network connection error: {ex.Message}" };
            }
        }

        public async Task<AdminDashboardData?> GetAdminDashboardAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<AdminDashboardData>($"{BaseUrl}/api/v1/dashboard/admin");
            }
            catch
            {
                UpdateConnectivity(false);
                return null;
            }
        }

        public async Task<UserDashboardData?> GetUserDashboardAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<UserDashboardData>($"{BaseUrl}/api/v1/dashboard/user");
            }
            catch
            {
                UpdateConnectivity(false);
                return null;
            }
        }

        public async Task<List<LocalDonation>> GetDonationsAsync(string? status = null, string? search = null)
        {
            try
            {
                var url = $"{BaseUrl}/api/v1/donations?status={status}&search={search}";
                var list = await _http.GetFromJsonAsync<List<LocalDonation>>(url);
                return list ?? new List<LocalDonation>();
            }
            catch
            {
                UpdateConnectivity(false);
                return new List<LocalDonation>();
            }
        }

        public async Task<List<LocalCause>> GetCausesAsync()
        {
            try
            {
                var list = await _http.GetFromJsonAsync<List<LocalCause>>($"{BaseUrl}/api/v1/causes");
                return list ?? new List<LocalCause>();
            }
            catch
            {
                UpdateConnectivity(false);
                return new List<LocalCause>();
            }
        }

        public async Task<bool> CreateCauseAsync(LocalCause cause)
        {
            try
            {
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/causes", cause);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                UpdateConnectivity(false);
                return false;
            }
        }

        public async Task<bool> UpdateCauseAsync(LocalCause cause)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"{BaseUrl}/api/v1/causes/{cause.Id}", cause);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                UpdateConnectivity(false);
                return false;
            }
        }

        public async Task<List<LocalProgramme>> GetProgrammesAsync()
        {
            try
            {
                var list = await _http.GetFromJsonAsync<List<LocalProgramme>>($"{BaseUrl}/api/v1/programmes");
                return list ?? new List<LocalProgramme>();
            }
            catch
            {
                UpdateConnectivity(false);
                return new List<LocalProgramme>();
            }
        }

        public async Task<bool> CreateProgrammeAsync(LocalProgramme programme)
        {
            try
            {
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/programmes", programme);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                UpdateConnectivity(false);
                return false;
            }
        }

        public async Task<bool> UpdateProgrammeAsync(LocalProgramme programme)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"{BaseUrl}/api/v1/programmes/{programme.Id}", programme);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                UpdateConnectivity(false);
                return false;
            }
        }

        public async Task<List<LocalQuery>> GetQueriesAsync(string? status = null)
        {
            try
            {
                var list = await _http.GetFromJsonAsync<List<LocalQuery>>($"{BaseUrl}/api/v1/queries?status={status}");
                return list ?? new List<LocalQuery>();
            }
            catch
            {
                UpdateConnectivity(false);
                return new List<LocalQuery>();
            }
        }

        public async Task<bool> CreateQueryAsync(LocalQuery query)
        {
            try
            {
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/queries", query);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                UpdateConnectivity(false);
                return false;
            }
        }

        public async Task<bool> ReplyQueryAsync(int queryId, string reply)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"{BaseUrl}/api/v1/queries/{queryId}/reply", reply);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                UpdateConnectivity(false);
                return false;
            }
        }

        public async Task<SyncPullData?> PullSyncAsync(DateTime? since = null)
        {
            try
            {
                var url = $"{BaseUrl}/api/v1/sync/pull";
                if (since.HasValue) url += $"?since={since.Value:O}";
                return await _http.GetFromJsonAsync<SyncPullData>(url);
            }
            catch
            {
                UpdateConnectivity(false);
                return null;
            }
        }

        public async Task<List<SyncResult>> PushSyncAsync(List<PendingOperation> operations)
        {
            try
            {
                var payload = new { Operations = operations };
                var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/sync/push", payload);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<SyncResult>>() ?? new List<SyncResult>();
                }
                return new List<SyncResult>();
            }
            catch
            {
                UpdateConnectivity(false);
                return new List<SyncResult>();
            }
        }
    }
}
