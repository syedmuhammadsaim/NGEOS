using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;
using NGEOS.Desktop.Infrastructure.Database;

namespace NGEOS.Desktop.Infrastructure.Services
{
    public class SyncService : ISyncService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnectivityService _connectivity;
        private readonly IAuditService _auditService;
        private readonly HttpClient _httpClient;

        public bool IsSyncing { get; private set; }
        public DateTime? LastSyncTimeUtc { get; private set; }
        public int PendingCount { get; private set; }

        public event Action? SyncStateChanged;

        public SyncService(IServiceScopeFactory scopeFactory, IConnectivityService connectivity, IAuditService auditService)
        {
            _scopeFactory = scopeFactory;
            _connectivity = connectivity;
            _auditService = auditService;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            // Auto-sync when internet comes back online
            _connectivity.ConnectivityChanged += OnConnectivityChanged;
        }

        private void OnConnectivityChanged(bool isOnline)
        {
            if (isOnline)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000); // Give connection 2 seconds to stabilize
                    await SynchronizeNowAsync();
                });
            }
        }

        public async Task QueueOperationAsync(string entityType, string actionType, string entityLocalId, object payload)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NgeosDbContext>();

            var json = JsonSerializer.Serialize(payload);
            var item = new SyncQueueItem
            {
                OperationId = Guid.NewGuid().ToString("N"),
                EntityType = entityType,
                ActionType = actionType,
                EntityLocalId = entityLocalId,
                JsonPayload = json,
                CreatedAtUtc = DateTime.UtcNow,
                SyncStatus = "Pending"
            };

            await db.SyncQueue.AddAsync(item);
            await db.SaveChangesAsync();

            await UpdatePendingCountAsync();

            // If we are online, trigger immediate sync
            if (_connectivity.IsOnline && !IsSyncing)
            {
                _ = SynchronizeNowAsync();
            }
        }

        public async Task<int> GetPendingCountAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NgeosDbContext>();
            return await db.SyncQueue.CountAsync(s => s.SyncStatus == "Pending" || s.SyncStatus == "Failed");
        }

        private async Task UpdatePendingCountAsync()
        {
            PendingCount = await GetPendingCountAsync();
            SyncStateChanged?.Invoke();
        }

        public async Task<(bool Success, string Message, int SyncedCount)> SynchronizeNowAsync()
        {
            if (IsSyncing)
                return (false, "Synchronization is already in progress.", 0);

            var isOnline = await _connectivity.CheckConnectivityAsync();
            if (!isOnline)
            {
                await UpdatePendingCountAsync();
                return (false, "Application is currently OFFLINE. All operations are preserved safely in local SQLite queue.", 0);
            }

            IsSyncing = true;
            SyncStateChanged?.Invoke();

            int syncedCount = 0;

            try
            {
                var baseUrl = _connectivity.RemoteApiBaseUrl.TrimEnd('/');

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NgeosDbContext>();

                // ================= PHASE 1: PUSH LOCAL PENDING OPERATIONS =================
                var pendingItems = await db.SyncQueue
                    .Where(s => s.SyncStatus == "Pending" || s.SyncStatus == "Failed")
                    .OrderBy(s => s.CreatedAtUtc)
                    .Take(50)
                    .ToListAsync();

                if (pendingItems.Count > 0)
                {
                    var pushRequest = new SyncPushRequestDto
                    {
                        Operations = pendingItems.Select(p => new SyncOperationDto
                        {
                            OperationId = p.OperationId,
                            EntityType = p.EntityType,
                            ActionType = p.ActionType,
                            JsonPayload = p.JsonPayload,
                            CreatedAtUtc = p.CreatedAtUtc
                        }).ToList()
                    };

                    var pushUrl = $"{baseUrl}/api/v1/sync/push";
                    var pushResponse = await _httpClient.PostAsJsonAsync(pushUrl, pushRequest);

                    if (pushResponse.IsSuccessStatusCode)
                    {
                        var pushResults = await pushResponse.Content.ReadFromJsonAsync<List<SyncPushResultDto>>();
                        if (pushResults != null)
                        {
                            foreach (var res in pushResults)
                            {
                                var queueItem = pendingItems.FirstOrDefault(p => p.OperationId == res.OperationId);
                                if (queueItem != null)
                                {
                                    if (res.Success)
                                    {
                                        queueItem.SyncStatus = "Synced";
                                        queueItem.SyncedAtUtc = DateTime.UtcNow;
                                        queueItem.ErrorMessage = null;
                                        syncedCount++;

                                        // Update local entity status
                                        if (queueItem.EntityType == "Donation" && int.TryParse(queueItem.EntityLocalId, out int donId))
                                        {
                                            var d = await db.Donations.FindAsync(donId);
                                            if (d != null)
                                            {
                                                d.IsSynced = true;
                                                d.ServerId = res.ServerEntityId;
                                            }
                                        }
                                        else if (queueItem.EntityType == "Cause" && int.TryParse(queueItem.EntityLocalId, out int causeId))
                                        {
                                            var c = await db.Causes.FindAsync(causeId);
                                            if (c != null)
                                            {
                                                c.IsSynced = true;
                                                c.ServerId = res.ServerEntityId;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        queueItem.SyncStatus = "Failed";
                                        queueItem.RetryCount++;
                                        queueItem.ErrorMessage = res.Message;
                                    }
                                }
                            }
                            await db.SaveChangesAsync();
                        }
                    }
                }

                // ================= PHASE 2: PULL REMOTE UPDATES =================
                var pullUrl = $"{baseUrl}/api/v1/sync/pull";
                if (LastSyncTimeUtc.HasValue)
                {
                    pullUrl += $"?since={LastSyncTimeUtc.Value:O}";
                }

                var pullResponse = await _httpClient.GetAsync(pullUrl);
                if (pullResponse.IsSuccessStatusCode)
                {
                    var pullData = await pullResponse.Content.ReadFromJsonAsync<SyncPullResponseDto>();
                    if (pullData != null)
                    {
                        // 1. Sync Remote Causes
                        if (pullData.Causes != null)
                        {
                            foreach (var sc in pullData.Causes)
                            {
                                var local = await db.Causes.FirstOrDefaultAsync(c => c.Title.ToLower() == sc.Title.ToLower());
                                if (local != null)
                                {
                                    // Update amounts if server has newer numbers
                                    if (sc.RaisedAmount > local.RaisedAmount)
                                        local.RaisedAmount = sc.RaisedAmount;
                                    local.ServerId = sc.Id;
                                    local.IsSynced = true;
                                }
                                else
                                {
                                    db.Causes.Add(new Cause
                                    {
                                        Title = sc.Title,
                                        Description = sc.ShortDescription ?? sc.FullDescription ?? "",
                                        Category = sc.Category,
                                        TargetAmount = sc.TargetAmount,
                                        RaisedAmount = sc.RaisedAmount,
                                        BeneficiariesCount = 300,
                                        IsActive = sc.IsActive,
                                        IsSynced = true,
                                        ServerId = sc.Id,
                                        CreatedAt = sc.CreatedAt != default ? sc.CreatedAt : DateTime.UtcNow
                                    });
                                }
                            }
                        }

                        // 2. Sync Remote Donations (Idempotency check by TransactionId)
                        if (pullData.Donations != null)
                        {
                            foreach (var sd in pullData.Donations)
                            {
                                var exists = await db.Donations.AnyAsync(d => d.TransactionId == sd.TransactionId);
                                if (!exists)
                                {
                                    var cause = await db.Causes.FirstOrDefaultAsync(c => c.Title == sd.CauseTitle)
                                                 ?? await db.Causes.FirstOrDefaultAsync();

                                    db.Donations.Add(new Donation
                                    {
                                        TransactionId = sd.TransactionId,
                                        ReceiptNumber = $"RCP-SRV-{sd.Id}",
                                        DonorName = sd.DonorName,
                                        DonorEmail = sd.DonorEmail ?? "donor@online.org",
                                        Amount = sd.Amount,
                                        CauseId = cause?.Id ?? 1,
                                        CauseTitle = sd.CauseTitle,
                                        PaymentMethod = sd.PaymentGateway ?? "Online Sync",
                                        PaymentStatus = sd.PaymentStatus ?? "Completed",
                                        DonationDate = sd.DonationDate != default ? sd.DonationDate : DateTime.UtcNow,
                                        IsSynced = true,
                                        ServerId = sd.Id
                                    });
                                }
                            }
                        }

                        await db.SaveChangesAsync();
                        LastSyncTimeUtc = pullData.ServerTimeUtc != default ? pullData.ServerTimeUtc : DateTime.UtcNow;
                    }
                }

                await _auditService.LogAsync("HybridSync", "SyncEngine", null, $"Two-way synchronization completed. {syncedCount} local changes pushed.");
                await UpdatePendingCountAsync();

                return (true, $"Synchronization completed! {syncedCount} pending change(s) synchronized with server.", syncedCount);
            }
            catch (Exception ex)
            {
                await UpdatePendingCountAsync();
                return (false, $"Synchronization paused: {ex.Message}. Local records remain safe and unaffected.", 0);
            }
            finally
            {
                IsSyncing = false;
                SyncStateChanged?.Invoke();
            }
        }
    }

    // Server-compatible DTOs
    public class SyncPushRequestDto
    {
        public List<SyncOperationDto> Operations { get; set; } = new();
    }

    public class SyncOperationDto
    {
        public string OperationId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string JsonPayload { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }

    public class SyncPushResultDto
    {
        public string OperationId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ServerEntityId { get; set; }
    }

    public class SyncPullResponseDto
    {
        public DateTime ServerTimeUtc { get; set; }
        public List<RemoteCauseDto>? Causes { get; set; }
        public List<RemoteDonationDto>? Donations { get; set; }
    }

    public class RemoteCauseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? FullDescription { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal RaisedAmount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RemoteDonationDto
    {
        public int Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string DonorName { get; set; } = string.Empty;
        public string? DonorEmail { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentGateway { get; set; }
        public string? PaymentStatus { get; set; }
        public string CauseTitle { get; set; } = string.Empty;
        public DateTime DonationDate { get; set; }
    }
}
