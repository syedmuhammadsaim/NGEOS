using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NGEOS.Desktop.Models;
using NGEOS.Desktop.Security;

namespace NGEOS.Desktop.Data
{
    public class OfflineDbContext : DbContext
    {
        public DbSet<LocalUser> Users => Set<LocalUser>();
        public DbSet<LocalDonation> Donations => Set<LocalDonation>();
        public DbSet<LocalCause> Causes => Set<LocalCause>();
        public DbSet<LocalProgramme> Programmes => Set<LocalProgramme>();
        public DbSet<LocalQuery> Queries => Set<LocalQuery>();
        public DbSet<PendingOperation> PendingOperations => Set<PendingOperation>();

        public static string GetDatabasePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "NGEOS");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "ngeos_offline.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var dbPath = GetDatabasePath();
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<PendingOperation>().HasIndex(p => p.OperationId).IsUnique();
        }

        public static void SeedInitialData(OfflineDbContext db, ISecureStorageService secureStorage)
        {
            try
            {
                if (!db.Users.Any())
                {
                    var admin = new LocalUser
                    {
                        Id = 1,
                        Email = "admin@ngeos.org",
                        FullName = "Syed Saim (Admin)",
                        Role = "Admin",
                        Profession = "NGO Director",
                        Organization = "NGEOS Central",
                        LastLoginAt = DateTime.UtcNow
                    };

                    var user = new LocalUser
                    {
                        Id = 2,
                        Email = "user@ngeos.org",
                        FullName = "Community Donor",
                        Role = "User",
                        Profession = "Philanthropist",
                        Organization = "Tech for Good",
                        LastLoginAt = DateTime.UtcNow
                    };

                    db.Users.AddRange(admin, user);

                    db.Causes.AddRange(
                        new LocalCause { Id = 1, Title = "Universal Education for Underprivileged Children", Category = "Education", TargetAmount = 500000, RaisedAmount = 375000, IsActive = true, IsFeatured = true },
                        new LocalCause { Id = 2, Title = "Clean Drinking Water & Solar Wells", Category = "Water Relief", TargetAmount = 800000, RaisedAmount = 620000, IsActive = true, IsFeatured = true },
                        new LocalCause { Id = 3, Title = "Emergency Medical & Patient Aid Relief", Category = "Healthcare", TargetAmount = 1200000, RaisedAmount = 910000, IsActive = true, IsFeatured = true },
                        new LocalCause { Id = 4, Title = "Women Vocational & Livelihood Center", Category = "Livelihood", TargetAmount = 450000, RaisedAmount = 310000, IsActive = true }
                    );

                    db.Programmes.AddRange(
                        new LocalProgramme { Id = 1, Title = "Youth Digital Literacy Bootcamp 2026", Category = "Education", Location = "Karachi & Lahore", TargetBeneficiaries = 300, IsActive = true },
                        new LocalProgramme { Id = 2, Title = "Mobile Health Clinic & Free Diagnostic Camps", Category = "Healthcare", Location = "Rural Sindh", TargetBeneficiaries = 1500, IsActive = true },
                        new LocalProgramme { Id = 3, Title = "Winter Warmth Clothing Drive", Category = "Relief", Location = "Northern Pakistan", TargetBeneficiaries = 600, IsActive = true }
                    );

                    db.Donations.AddRange(
                        new LocalDonation { Id = 1, TransactionId = "NGEOS-TXN-8801", DonorName = "Ahmed Khan", DonorEmail = "ahmed.k@example.com", Amount = 50000, CauseTitle = "Universal Education", PaymentStatus = "Completed", DonationDate = DateTime.UtcNow.AddDays(-2) },
                        new LocalDonation { Id = 2, TransactionId = "NGEOS-TXN-8802", DonorName = "Fatima Noor", DonorEmail = "fatima@example.com", Amount = 25000, CauseTitle = "Clean Drinking Water", PaymentStatus = "Completed", DonationDate = DateTime.UtcNow.AddDays(-1) },
                        new LocalDonation { Id = 3, TransactionId = "NGEOS-TXN-8803", DonorName = "Zaid Ali", DonorEmail = "zaid.ali@example.com", Amount = 100000, CauseTitle = "Emergency Medical", PaymentStatus = "Completed", DonationDate = DateTime.UtcNow.AddHours(-12) },
                        new LocalDonation { Id = 4, TransactionId = "NGEOS-TXN-8804", DonorName = "Syed Saim", DonorEmail = "admin@ngeos.org", Amount = 35000, CauseTitle = "Universal Education", PaymentStatus = "Completed", DonationDate = DateTime.UtcNow.AddHours(-4) }
                    );

                    db.Queries.AddRange(
                        new LocalQuery { Id = 1, FullName = "Bilal Tariq", Email = "bilal@example.com", Subject = "Volunteer registration process", Message = "How can our student team volunteer for the education drive?", Status = "Replied", AdminReply = "Please visit our Programmes tab and apply directly under Youth Bootcamp.", CreatedAt = DateTime.UtcNow.AddDays(-3) },
                        new LocalQuery { Id = 2, FullName = "Hina Mansoor", Email = "hina@example.com", Subject = "Tax exemption certificate request", Message = "Could you please provide the formal tax exempt receipt for donation NGEOS-TXN-8802?", Status = "Pending", CreatedAt = DateTime.UtcNow.AddDays(-1) }
                    );

                    db.SaveChanges();
                }

                // Pre-seed offline DPAPI credentials
                var adminPassHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("Admin@123")));
                secureStorage.SaveOfflineCredentials("admin@ngeos.org", adminPassHash, "Admin", 1, "Syed Saim (Admin)");

                var userPassHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("User@123")));
                secureStorage.SaveOfflineCredentials("user@ngeos.org", userPassHash, "User", 2, "Community Donor");
            }
            catch { }
        }
    }
}
