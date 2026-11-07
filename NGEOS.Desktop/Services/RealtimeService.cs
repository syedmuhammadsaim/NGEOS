using Microsoft.AspNetCore.SignalR.Client;
using NGEOS.Desktop.Security;

namespace NGEOS.Desktop.Services
{
    public interface IRealtimeService
    {
        bool IsConnected { get; }
        event EventHandler<string>? RealtimeNotificationReceived;
        event EventHandler? DashboardRefreshRequested;
        event EventHandler? CauseUpdated;

        Task StartAsync();
        Task StopAsync();
    }

    public class RealtimeService : IRealtimeService
    {
        private readonly IApiService _apiService;
        private readonly ISecureStorageService _secureStorage;
        private HubConnection? _hubConnection;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        public event EventHandler<string>? RealtimeNotificationReceived;
        public event EventHandler? DashboardRefreshRequested;
        public event EventHandler? CauseUpdated;

        public RealtimeService(IApiService apiService, ISecureStorageService secureStorage)
        {
            _apiService = apiService;
            _secureStorage = secureStorage;
        }

        public async Task StartAsync()
        {
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.DisposeAsync();
                }

                var hubUrl = $"{_apiService.BaseUrl}/hubs/ngeos";
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        var token = _secureStorage.GetToken();
                        if (!string.IsNullOrEmpty(token))
                        {
                            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                        }
                    })
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15) })
                    .Build();

                _hubConnection.On<string>("ReceiveNotification", message =>
                {
                    RealtimeNotificationReceived?.Invoke(this, message);
                });

                _hubConnection.On("ReceiveDashboardRefresh", () =>
                {
                    DashboardRefreshRequested?.Invoke(this, EventArgs.Empty);
                });

                _hubConnection.On<object>("ReceiveCauseUpdate", _ =>
                {
                    CauseUpdated?.Invoke(this, EventArgs.Empty);
                });

                await _hubConnection.StartAsync();
            }
            catch
            {
                // SignalR will retry when online or called again
            }
        }

        public async Task StopAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }
    }
}
