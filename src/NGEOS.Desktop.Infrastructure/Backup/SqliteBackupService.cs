using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;
using NGEOS.Desktop.Infrastructure.Database;

namespace NGEOS.Desktop.Infrastructure.Backup
{
    public class SqliteBackupService : IDatabaseBackupService
    {
        private readonly NgeosDbContext _context;
        private readonly IAuditService _auditService;

        public string BackupDirectory { get; }

        public SqliteBackupService(NgeosDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;

            BackupDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NGEOS_Desktop",
                "Backups"
            );

            if (!Directory.Exists(BackupDirectory))
            {
                Directory.CreateDirectory(BackupDirectory);
            }
        }

        public async Task<BackupRecord> CreateBackupAsync(bool isAutomatic = false)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var prefix = isAutomatic ? "NGEOS_AutoBackup" : "NGEOS_ManualBackup";
            var fileName = $"{prefix}_{timestamp}.db";
            var destinationPath = Path.Combine(BackupDirectory, fileName);

            try
            {
                // In SQLite, VACUUM INTO creates a transactionally consistent, compact copy
                // even while other queries or WAL journals are active.
                var escapedPath = destinationPath.Replace("'", "''");
                await _context.Database.ExecuteSqlRawAsync($"VACUUM INTO '{escapedPath}';");

                var fileInfo = new FileInfo(destinationPath);
                var record = new BackupRecord
                {
                    BackupPath = destinationPath,
                    FileSizeBytes = fileInfo.Length,
                    Status = "Success",
                    IsAutomatic = isAutomatic,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.BackupRecords.AddAsync(record);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("BackupCreated", "Database", record.Id.ToString(), $"Database backup created: {fileName} ({record.FormattedSize}).");
                return record;
            }
            catch (Exception ex)
            {
                var record = new BackupRecord
                {
                    BackupPath = destinationPath,
                    FileSizeBytes = 0,
                    Status = $"Failed: {ex.Message}",
                    IsAutomatic = isAutomatic,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.BackupRecords.AddAsync(record);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("BackupFailed", "Database", null, $"Backup creation failed: {ex.Message}");
                throw;
            }
        }

        public async Task<(bool Success, string Message)> RestoreBackupAsync(string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
                return (false, "Specified backup file does not exist.");

            // 1. Verify SQLite Header Magic Bytes ("SQLite format 3\0")
            try
            {
                using var fs = new FileStream(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var headerBytes = new byte[16];
                if (fs.Read(headerBytes, 0, 16) < 16)
                    return (false, "File is too small to be a valid SQLite database.");

                var headerStr = Encoding.ASCII.GetString(headerBytes);
                if (!headerStr.StartsWith("SQLite format 3"))
                    return (false, "Selected file is corrupted or not a valid SQLite database.");
            }
            catch (Exception ex)
            {
                return (false, $"Unable to read backup file: {ex.Message}");
            }

            var liveDbPath = NgeosDbContext.GetDatabasePath();
            var safetyBackupPath = Path.Combine(BackupDirectory, $"PreRestore_SafetyBackup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

            try
            {
                // 2. Create Pre-Restore Safety Backup of Current Live DB
                if (File.Exists(liveDbPath))
                {
                    var escapedSafety = safetyBackupPath.Replace("'", "''");
                    await _context.Database.ExecuteSqlRawAsync($"VACUUM INTO '{escapedSafety}';");
                }

                // 3. Clear SQLite Pools and close active connections
                SqliteConnection.ClearAllPools();
                await _context.Database.CloseConnectionAsync();

                // 4. Overwrite live DB and remove stale WAL / SHM files
                File.Copy(backupFilePath, liveDbPath, overwrite: true);

                var walPath = liveDbPath + "-wal";
                var shmPath = liveDbPath + "-shm";
                if (File.Exists(walPath)) File.Delete(walPath);
                if (File.Exists(shmPath)) File.Delete(shmPath);

                // 5. Test Integrity Check on restored DB
                await _context.Database.OpenConnectionAsync();
                var integrity = await _context.Database.SqlQueryRaw<string>("PRAGMA integrity_check;").ToListAsync();
                if (integrity == null || integrity.Count == 0 || !integrity[0].Equals("ok", StringComparison.OrdinalIgnoreCase))
                {
                    // Rollback from safety backup
                    SqliteConnection.ClearAllPools();
                    await _context.Database.CloseConnectionAsync();
                    File.Copy(safetyBackupPath, liveDbPath, overwrite: true);
                    return (false, "Integrity check failed on restored database. Restored previous safety state.");
                }

                await _auditService.LogAsync("BackupRestored", "Database", null, $"Database successfully restored from: {Path.GetFileName(backupFilePath)}.");
                return (true, "Database has been restored successfully and verified with integrity checks.");
            }
            catch (Exception ex)
            {
                // Rollback if possible
                try
                {
                    if (File.Exists(safetyBackupPath))
                    {
                        SqliteConnection.ClearAllPools();
                        File.Copy(safetyBackupPath, liveDbPath, overwrite: true);
                    }
                }
                catch { }

                return (false, $"Restore failed: {ex.Message}");
            }
        }

        public async Task<IReadOnlyList<BackupRecord>> GetBackupHistoryAsync()
        {
            return await _context.BackupRecords
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task DeleteBackupAsync(int backupId)
        {
            var record = await _context.BackupRecords.FindAsync(backupId);
            if (record != null)
            {
                if (File.Exists(record.BackupPath))
                {
                    try { File.Delete(record.BackupPath); } catch { }
                }

                _context.BackupRecords.Remove(record);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("BackupDeleted", "Database", backupId.ToString(), "Backup archive deleted.");
            }
        }
    }
}
