using System;

namespace NGEOS.Desktop.Core.Entities
{
    public class Programme
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Relief";
        public string Location { get; set; } = string.Empty;
        public int TargetBeneficiaries { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Query
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Replied, Resolved
        public string? AdminReply { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RepliedAt { get; set; }
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // Login, Logout, Create, Update, Delete, Export, Backup
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class BackupRecord
    {
        public int Id { get; set; }
        public string BackupPath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string Status { get; set; } = "Success"; // Success, Failed
        public bool IsAutomatic { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string FormattedSize => FileSizeBytes < 1024 * 1024 
            ? $"{(FileSizeBytes / 1024.0):F1} KB" 
            : $"{(FileSizeBytes / (1024.0 * 1024.0)):F2} MB";
    }
}
