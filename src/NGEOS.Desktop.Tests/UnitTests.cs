using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;
using NGEOS.Desktop.Infrastructure.Backup;
using NGEOS.Desktop.Infrastructure.Database;
using NGEOS.Desktop.Infrastructure.Export;
using NGEOS.Desktop.Infrastructure.Security;
using NGEOS.Desktop.Infrastructure.Services;
using Xunit;

namespace NGEOS.Desktop.Tests
{
    public class PasswordSecurityTests
    {
        private readonly IPasswordHasher _hasher = new Pbkdf2PasswordHasher();

        [Fact]
        public void HashPassword_ShouldReturnValidHashAnd32ByteSalt()
        {
            var (hash, salt) = _hasher.HashPassword("SecretPassword123!");

            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.False(string.IsNullOrWhiteSpace(salt));

            var saltBytes = Convert.FromHexString(salt);
            Assert.Equal(32, saltBytes.Length);
        }

        [Fact]
        public void VerifyPassword_ShouldSucceedForCorrectPassword()
        {
            var pwd = "ComplexPassword#2026";
            var (hash, salt) = _hasher.HashPassword(pwd);

            var isValid = _hasher.VerifyPassword(pwd, hash, salt);

            Assert.True(isValid);
        }

        [Fact]
        public void VerifyPassword_ShouldFailForIncorrectPassword()
        {
            var (hash, salt) = _hasher.HashPassword("CorrectPassword123");

            var isValid = _hasher.VerifyPassword("WrongPassword456", hash, salt);

            Assert.False(isValid);
        }

        [Fact]
        public void VerifyPassword_ShouldSupportDefaultAdminCredential()
        {
            var (hash, salt) = _hasher.HashPassword("Admin@12");
            var isValid = _hasher.VerifyPassword("Admin@12", hash, salt);
            Assert.True(isValid);
        }
    }

    public class DatabasePersistenceTests : IDisposable
    {
        private readonly string _testDbPath;

        public DatabasePersistenceTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"ngeos_test_{Guid.NewGuid():N}.db");
        }

        [Fact]
        public async Task Database_ShouldInitializeAndSeedSuccessfully()
        {
            var hasher = new Pbkdf2PasswordHasher();
            using var context = new TestNgeosDbContext(_testDbPath);

            await context.InitializeAndSeedAsync(hasher);

            var usersCount = await context.Users.CountAsync();
            var causesCount = await context.Causes.CountAsync();
            var donationsCount = await context.Donations.CountAsync();

            Assert.True(usersCount >= 2);
            Assert.True(causesCount >= 4);
            Assert.True(donationsCount >= 4);
        }

        [Fact]
        public async Task Database_ShouldPersistNewCauseAndDonation()
        {
            var hasher = new Pbkdf2PasswordHasher();
            using var context = new TestNgeosDbContext(_testDbPath);
            await context.InitializeAndSeedAsync(hasher);

            // Add new Cause
            var newCause = new Cause
            {
                Title = "Test Orphan Care Initiative",
                Description = "Supporting orphan care centers with nutrition and education.",
                Category = "Child Welfare",
                TargetAmount = 300000m,
                RaisedAmount = 0,
                BeneficiariesCount = 100,
                IsActive = true
            };
            await context.Causes.AddAsync(newCause);
            await context.SaveChangesAsync();

            // Add new Donation
            var donation = new Donation
            {
                TransactionId = "TXN-TEST-9999",
                ReceiptNumber = "RCP-TEST-9999",
                DonorName = "Philanthropist Tester",
                DonorEmail = "tester@example.org",
                Amount = 50000m,
                CauseId = newCause.Id,
                CauseTitle = newCause.Title,
                PaymentMethod = "Direct Wire",
                PaymentStatus = "Completed",
                DonationDate = DateTime.UtcNow
            };
            await context.Donations.AddAsync(donation);
            await context.SaveChangesAsync();

            // Verify retrieval
            var retrievedCause = await context.Causes.FirstOrDefaultAsync(c => c.Title == "Test Orphan Care Initiative");
            var retrievedDonation = await context.Donations.FirstOrDefaultAsync(d => d.TransactionId == "TXN-TEST-9999");

            Assert.NotNull(retrievedCause);
            Assert.NotNull(retrievedDonation);
            Assert.Equal(50000m, retrievedDonation.Amount);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
                var wal = _testDbPath + "-wal";
                var shm = _testDbPath + "-shm";
                if (File.Exists(wal)) File.Delete(wal);
                if (File.Exists(shm)) File.Delete(shm);
            }
            catch { }
        }
    }

    public class ExportAndReportTests
    {
        [Fact]
        public void ClosedXml_ShouldGenerateValidExcelWorkbook()
        {
            var service = new ClosedXmlExportService();
            var donations = new[]
            {
                new Donation
                {
                    TransactionId = "TXN-EXCEL-01",
                    ReceiptNumber = "RCP-01",
                    DonorName = "Test Donor",
                    DonorEmail = "test@ngo.org",
                    Amount = 25000m,
                    CauseTitle = "Clean Water",
                    PaymentMethod = "Bank",
                    PaymentStatus = "Completed",
                    DonationDate = DateTime.UtcNow
                }
            };

            var bytes = service.ExportDonationsToExcel(donations);

            Assert.NotNull(bytes);
            Assert.NotEmpty(bytes);
            // OpenXML files are ZIP archives starting with PK (0x50 0x4B 0x03 0x04)
            Assert.Equal((byte)'P', bytes[0]);
            Assert.Equal((byte)'K', bytes[1]);
        }

        [Fact]
        public void QuestPdf_ShouldGenerateValidPdfDocument()
        {
            var service = new QuestPdfReportService();
            var donation = new Donation
            {
                TransactionId = "TXN-PDF-01",
                ReceiptNumber = "RCP-PDF-01",
                DonorName = "Dr. Saim Test",
                DonorEmail = "saim@example.org",
                Amount = 100000m,
                CauseTitle = "Universal Education",
                PaymentMethod = "PayPak Card",
                PaymentStatus = "Completed",
                DonationDate = DateTime.UtcNow
            };

            var pdfBytes = service.GenerateDonationReceiptPdf(donation);

            Assert.NotNull(pdfBytes);
            Assert.NotEmpty(pdfBytes);
            // PDF file starts with %PDF-
            var header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
            Assert.Equal("%PDF-", header);
        }
    }

    public class HybridSyncTests : IDisposable
    {
        private readonly string _testDbPath;

        public HybridSyncTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"ngeos_synctest_{Guid.NewGuid():N}.db");
        }

        [Fact]
        public async Task SyncQueue_ShouldEnqueuePendingOperations()
        {
            using var context = new TestNgeosDbContext(_testDbPath);
            await context.Database.EnsureCreatedAsync();

            var item = new SyncQueueItem
            {
                OperationId = Guid.NewGuid().ToString("N"),
                EntityType = "Donation",
                ActionType = "Create",
                EntityLocalId = "101",
                JsonPayload = "{\"Amount\": 50000, \"DonorName\": \"Syed Saim\"}",
                CreatedAtUtc = DateTime.UtcNow,
                SyncStatus = "Pending"
            };

            await context.SyncQueue.AddAsync(item);
            await context.SaveChangesAsync();

            var pendingCount = await context.SyncQueue.CountAsync(s => s.SyncStatus == "Pending");
            Assert.Equal(1, pendingCount);

            var retrieved = await context.SyncQueue.FirstOrDefaultAsync(s => s.EntityLocalId == "101");
            Assert.NotNull(retrieved);
            Assert.Equal("Pending", retrieved.SyncStatus);
        }

        [Fact]
        public async Task SyncQueue_StatusTransition_ShouldTrackSuccessfulSync()
        {
            using var context = new TestNgeosDbContext(_testDbPath);
            await context.Database.EnsureCreatedAsync();

            var item = new SyncQueueItem
            {
                OperationId = "OP-TEST-44",
                EntityType = "Cause",
                ActionType = "Create",
                EntityLocalId = "202",
                JsonPayload = "{\"Title\": \"Offline Cause\"}",
                CreatedAtUtc = DateTime.UtcNow,
                SyncStatus = "Pending"
            };
            await context.SyncQueue.AddAsync(item);
            await context.SaveChangesAsync();

            // Simulate sync success transition
            item.SyncStatus = "Synced";
            item.SyncedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var pendingCount = await context.SyncQueue.CountAsync(s => s.SyncStatus == "Pending");
            var syncedItem = await context.SyncQueue.FirstOrDefaultAsync(s => s.OperationId == "OP-TEST-44");

            Assert.Equal(0, pendingCount);
            Assert.NotNull(syncedItem);
            Assert.Equal("Synced", syncedItem.SyncStatus);
            Assert.NotNull(syncedItem.SyncedAtUtc);
        }

        [Fact]
        public async Task LiveDatabase_ShouldHaveActiveCauses()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NGEOS_Desktop", "ngeos_desktop.db");
            if (!File.Exists(dbPath)) return;

            using var db = new TestNgeosDbContext(dbPath);
            var causes = await db.Causes.ToListAsync();
            Assert.True(causes.Count >= 4);
        }

        [Fact]
        public async Task InitializeAndSeedAsync_ShouldSeedCausesAndProgrammesIndependently()
        {
            var seedDbPath = Path.Combine(Path.GetTempPath(), $"ngeos_seed_test_{Guid.NewGuid():N}.db");
            try
            {
                using (var db = new TestNgeosDbContext(seedDbPath))
                {
                    var hasher = new Pbkdf2PasswordHasher();
                    await db.InitializeAndSeedAsync(hasher);

                    var causesCount = await db.Causes.CountAsync();
                    var progsCount = await db.Programmes.CountAsync();
                    var donorsCount = await db.Donations.CountAsync();
                    var usersCount = await db.Users.CountAsync();

                    Assert.True(causesCount >= 4, $"Expected at least 4 causes, but found {causesCount}");
                    Assert.True(progsCount >= 3, $"Expected at least 3 programmes, but found {progsCount}");
                    Assert.True(donorsCount >= 4, $"Expected at least 4 donations, but found {donorsCount}");
                    Assert.True(usersCount >= 2, $"Expected at least 2 users, but found {usersCount}");
                }
            }
            finally
            {
                try
                {
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                    if (File.Exists(seedDbPath)) File.Delete(seedDbPath);
                }
                catch { }
            }
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
            }
            catch { }
        }
    }

    internal class TestNgeosDbContext : NgeosDbContext
    {
        private readonly string _dbPath;

        public TestNgeosDbContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath};Cache=Shared;");
        }
    }
}
