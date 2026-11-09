using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NGEOS.Desktop.Core.Entities;

namespace NGEOS.Desktop.Core.Interfaces
{
    public interface IPasswordHasher
    {
        (string Hash, string Salt) HashPassword(string password);
        bool VerifyPassword(string password, string hash, string salt);
    }

    public interface IAuthService
    {
        User? CurrentUser { get; }
        event Action<User?>? AuthStateChanged;

        Task<(bool Success, string Message, User? User)> LoginAsync(string email, string password);
        Task<(bool Success, string Message, User? User)> RegisterAsync(string fullName, string email, string password, string profession, string organization, string? phone);
        Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<(bool Success, string Message)> ResetAdminPasswordAsync(string newPassword);
        void Logout();
    }

    public interface IDatabaseBackupService
    {
        string BackupDirectory { get; }
        Task<BackupRecord> CreateBackupAsync(bool isAutomatic = false);
        Task<(bool Success, string Message)> RestoreBackupAsync(string backupFilePath);
        Task<IReadOnlyList<BackupRecord>> GetBackupHistoryAsync();
        Task DeleteBackupAsync(int backupId);
    }

    public interface IReportService
    {
        byte[] GenerateDonationReceiptPdf(Donation donation);
        byte[] GenerateAuditReportPdf(IReadOnlyList<Donation> donations, IReadOnlyList<Cause> causes, decimal totalRaised, int totalDonors);
    }

    public interface IExcelExportService
    {
        byte[] ExportDonationsToExcel(IReadOnlyList<Donation> donations);
        byte[] ExportCausesToExcel(IReadOnlyList<Cause> causes);
    }

    public interface IAuditService
    {
        Task LogAsync(string action, string entityName, string? entityId, string details);
        Task<IReadOnlyList<AuditLog>> GetRecentLogsAsync(int count = 100);
    }

    public interface IConnectivityService
    {
        bool IsOnline { get; }
        string RemoteApiBaseUrl { get; set; }
        event Action<bool>? ConnectivityChanged;
        Task<bool> CheckConnectivityAsync();
        void StartMonitoring(int intervalSeconds = 15);
        void StopMonitoring();
    }

    public interface ISyncService
    {
        bool IsSyncing { get; }
        DateTime? LastSyncTimeUtc { get; }
        int PendingCount { get; }
        event Action? SyncStateChanged;

        Task QueueOperationAsync(string entityType, string actionType, string entityLocalId, object payload);
        Task<(bool Success, string Message, int SyncedCount)> SynchronizeNowAsync();
        Task<int> GetPendingCountAsync();
    }
}
