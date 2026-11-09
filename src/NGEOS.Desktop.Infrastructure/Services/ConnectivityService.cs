using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using NGEOS.Desktop.Core.Interfaces;

namespace NGEOS.Desktop.Infrastructure.Services
{
    public class ConnectivityService : IConnectivityService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private Timer? _timer;
        private bool _isOnline;
        private int _isChecking;

        public bool IsOnline
        {
            get => _isOnline;
            private set
            {
                if (_isOnline != value)
                {
                    _isOnline = value;
                    ConnectivityChanged?.Invoke(_isOnline);
                }
            }
        }

        public string RemoteApiBaseUrl { get; set; } = "https://localhost:5001";

        public event Action<bool>? ConnectivityChanged;

        public ConnectivityService()
        {
            var handler = new HttpClientHandler
            {
                // In local development or self-signed certs, accept certs safely
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(2.5)
            };

            // Register system network interface events
            try
            {
                NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            }
            catch { }
        }

        private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            // Trigger fast probe on interface state change
            _ = CheckConnectivityAsync();
        }

        public async Task<bool> CheckConnectivityAsync()
        {
            // Prevent multiple concurrent checks
            if (Interlocked.Exchange(ref _isChecking, 1) == 1)
                return IsOnline;

            try
            {
                // Quick hardware check
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    IsOnline = false;
                    return false;
                }

                // HTTP Heartbeat probe to server sync health check
                var healthUrl = $"{RemoteApiBaseUrl.TrimEnd('/')}/api/v1/sync/health";
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2.5));
                var response = await _httpClient.GetAsync(healthUrl, cts.Token);

                IsOnline = response.IsSuccessStatusCode;
                return IsOnline;
            }
            catch
            {
                IsOnline = false;
                return false;
            }
            finally
            {
                Interlocked.Exchange(ref _isChecking, 0);
            }
        }

        public void StartMonitoring(int intervalSeconds = 15)
        {
            StopMonitoring();
            _timer = new Timer(async _ =>
            {
                await CheckConnectivityAsync();
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(Math.Max(5, intervalSeconds)));
        }

        public void StopMonitoring()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose()
        {
            StopMonitoring();
            try
            {
                NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            }
            catch { }
            _httpClient.Dispose();
        }
    }
}
