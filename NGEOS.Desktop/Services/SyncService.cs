using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NGEOS.Desktop.Data;
using NGEOS.Desktop.Models;
using NGEOS.Desktop.Security;

namespace NGEOS.Desktop.Services
{
    public interface ISyncService
    {
        bool IsSyncing { get; }
        DateTime? LastSyncTime { get; }
        int PendingOperationsCount { get; }

        event EventHandler<SyncEventArgs>? SyncStarted;
        event EventHandler<SyncEventArgs>? SyncCompleted;
        event EventHandler<int>? PendingCountChanged;

        Task QueueOperationAsync(string entityType, string actionType, object payload);
        Task<SyncSummary> TriggerSyncAsync();
        Task StartAutoSyncAsync(CancellationToken cancellationToken);
    }

    public class SyncEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int SyncedCount { get; set; }
        public int FailedCount { get; set; }
    }

    public class SyncSummary
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PushedCount { get; set; }
        public int PulledCount { get; set; }
        public int FailedCount { get; set; }
    }

    public class SyncService : ISyncService
    {
        private readonly IApiService _apiService;
        private readonly ISecureStorageService _secureStorage;
        private bool _isSyncing;
        private DateTime? _lastSyncTime;
        private int _pendingCount;

        public bool IsSyncing => _isSyncing;
        public DateTime? LastSyncTime => _lastSyncTime;
        public int PendingOperationsCount => _pendingCount;

        public event EventHandler<SyncEventArgs>? SyncStarted;
        public event EventHandler<SyncEventArgs>? SyncCompleted;
        public event EventHandler<int>? PendingCountChanged;

        public SyncService(IApiService apiService, ISecureStorageService secureStorage)
        {
            _apiService = apiService;
            _secureStorage = secureStorage;

            var lastSyncStr = _secureStorage.GetSetting("last_sync_utc");
            if (DateTime.TryParse(lastSyncStr, out var dt))
            {
                _lastSyncTime = dt.ToLocalTime();
            }

            // Load initial pending count
            Task.Run(async () =>
            {
                using var db = new OfflineDbContext();
                await db.Database.EnsureCreatedAsync();
                _pendingCount = await db.PendingOperations.CountAsync(p => p.Status == "Pending");
                PendingCountChanged?.Invoke(this, _pendingCount);
            });

            // Listen to online status
            _apiService.ConnectivityChanged += async (_, isOnline) =>
            {
                if (isOnline)
                {
                    await TriggerSyncAsync();
                }
            };
        }

        public async Task QueueOperationAsync(string entityType, string actionType, object payload)
        {
            using var db = new OfflineDbContext();
            await db.Database.EnsureCreatedAsync();

            var op = new PendingOperation
            {
                OperationId = Guid.NewGuid().ToString("N"),
                EntityType = entityType,
                ActionType = actionType,
                JsonPayload = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTime.UtcNow,
                Status = "Pending"
            };

            db.PendingOperations.Add(op);
            await db.SaveChangesAsync();

            _pendingCount = await db.PendingOperations.CountAsync(p => p.Status == "Pending");
            PendingCountChanged?.Invoke(this, _pendingCount);

            // If online right now, try syncing immediately
            if (_apiService.IsOnline)
            {
                _ = TriggerSyncAsync();
            }
        }

        public async Task<SyncSummary> TriggerSyncAsync()
        {
            if (_isSyncing)
            {
                return new SyncSummary { Success = false, Message = "Synchronization is already in progress." };
            }

            _isSyncing = true;
            SyncStarted?.Invoke(this, new SyncEventArgs { Message = "Synchronization started..." });

            int pushedCount = 0;
            int failedCount = 0;
            int pulledCount = 0;

            try
            {
                var isOnline = await _apiService.CheckConnectivityAsync();
                if (!isOnline)
                {
                    _isSyncing = false;
                    SyncCompleted?.Invoke(this, new SyncEventArgs { Success = false, Message = "Device is offline. Changes are stored locally." });
                    return new SyncSummary { Success = false, Message = "Device is offline." };
                }

                using var db = new OfflineDbContext();
                await db.Database.EnsureCreatedAsync();

                // 1. PUSH: Send pending offline operations to Web API
                var pendingOps = await db.PendingOperations
                    .Where(p => p.Status == "Pending")
                    .OrderBy(p => p.CreatedAtUtc)
                    .Take(50)
                    .ToListAsync();

                if (pendingOps.Any())
                {
                    var results = await _apiService.PushSyncAsync(pendingOps);
                    foreach (var res in results)
                    {
                        var op = pendingOps.FirstOrDefault(p => p.OperationId == res.OperationId);
                        if (op != null)
                        {
                            if (res.Success)
                            {
                                op.Status = "Completed";
                                pushedCount++;
                            }
                            else
                            {
                                op.RetryCount++;
                                op.Status = op.RetryCount > 5 ? "Failed" : "Pending";
                                op.ErrorMessage = res.Message;
                                failedCount++;
                            }
                        }
                    }
                    await db.SaveChangesAsync();
                }

                // 2. PULL: Fetch latest updates from server
                var pullData = await _apiService.PullSyncAsync();
                if (pullData != null)
                {
                    // Merge causes
                    foreach (var c in pullData.Causes)
                    {
                        var localC = await db.Causes.FindAsync(c.Id);
                        if (localC == null)
                        {
                            db.Causes.Add(c);
                        }
                        else
                        {
                            localC.Title = c.Title;
                            localC.ShortDescription = c.ShortDescription;
                            localC.FullDescription = c.FullDescription;
                            localC.Category = c.Category;
                            localC.TargetAmount = c.TargetAmount;
                            localC.RaisedAmount = c.RaisedAmount;
                            localC.ImageUrl = c.ImageUrl;
                            localC.IsActive = c.IsActive;
                        }
                        pulledCount++;
                    }

                    // Merge programmes
                    foreach (var p in pullData.Programmes)
                    {
                        var localP = await db.Programmes.FindAsync(p.Id);
                        if (localP == null)
                        {
                            db.Programmes.Add(p);
                        }
                        else
                        {
                            localP.Title = p.Title;
                            localP.Category = p.Category;
                            localP.ShortDescription = p.ShortDescription;
                            localP.FullDescription = p.FullDescription;
                            localP.ImageUrl = p.ImageUrl;
                            localP.TargetBeneficiaries = p.TargetBeneficiaries;
                            localP.IsActive = p.IsActive;
                        }
                        pulledCount++;
                    }

                    // Merge donations
                    foreach (var d in pullData.Donations)
                    {
                        var localD = await db.Donations.FindAsync(d.Id);
                        if (localD == null)
                        {
                            db.Donations.Add(d);
                        }
                        else
                        {
                            localD.PaymentStatus = d.PaymentStatus;
                        }
                        pulledCount++;
                    }

                    // Merge queries
                    foreach (var q in pullData.Queries)
                    {
                        var localQ = await db.Queries.FindAsync(q.Id);
                        if (localQ == null)
                        {
                            db.Queries.Add(q);
                        }
                        else
                        {
                            localQ.AdminReply = q.AdminReply;
                            localQ.Status = q.Status;
                            localQ.RepliedAt = q.RepliedAt;
                        }
                        pulledCount++;
                    }

                    await db.SaveChangesAsync();
                }

                _lastSyncTime = DateTime.Now;
                _secureStorage.SaveSetting("last_sync_utc", DateTime.UtcNow.ToString("O"));

                _pendingCount = await db.PendingOperations.CountAsync(p => p.Status == "Pending");
                PendingCountChanged?.Invoke(this, _pendingCount);

                var summary = new SyncSummary
                {
                    Success = true,
                    Message = $"Sync complete: {pushedCount} uploaded, {pulledCount} updated.",
                    PushedCount = pushedCount,
                    PulledCount = pulledCount,
                    FailedCount = failedCount
                };

                _isSyncing = false;
                SyncCompleted?.Invoke(this, new SyncEventArgs
                {
                    Success = true,
                    Message = summary.Message,
                    SyncedCount = pushedCount + pulledCount,
                    FailedCount = failedCount
                });

                return summary;
            }
            catch (Exception ex)
            {
                _isSyncing = false;
                var msg = $"Synchronization encountered an error: {ex.Message}";
                SyncCompleted?.Invoke(this, new SyncEventArgs { Success = false, Message = msg });
                return new SyncSummary { Success = false, Message = msg, FailedCount = failedCount };
            }
        }

        public async Task StartAutoSyncAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(2), cancellationToken);
                    if (_apiService.IsOnline)
                    {
                        await TriggerSyncAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch
                {
                    // Ignore background interval errors
                }
            }
        }
    }
}
