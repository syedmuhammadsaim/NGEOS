using System;

namespace NGEOS.Desktop.Core.Entities
{
    public class SyncQueueItem
    {
        public int Id { get; set; }
        public string OperationId { get; set; } = Guid.NewGuid().ToString("N");
        public string EntityType { get; set; } = string.Empty; // "Donation", "Cause", "Query"
        public string ActionType { get; set; } = "Create";    // "Create", "Update", "Delete"
        public string EntityLocalId { get; set; } = string.Empty;
        public string JsonPayload { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string SyncStatus { get; set; } = "Pending";    // "Pending", "Syncing", "Synced", "Failed"
        public int RetryCount { get; set; } = 0;
        public string? ErrorMessage { get; set; }
        public DateTime? SyncedAtUtc { get; set; }
    }
}
