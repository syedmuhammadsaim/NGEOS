using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;

namespace NGEOS.Desktop.Infrastructure.Database
{
    public class NgeosDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Cause> Causes => Set<Cause>();
        public DbSet<Donation> Donations => Set<Donation>();
        public DbSet<Programme> Programmes => Set<Programme>();
        public DbSet<Query> Queries => Set<Query>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<BackupRecord> BackupRecords => Set<BackupRecord>();
        public DbSet<SyncQueueItem> SyncQueue => Set<SyncQueueItem>();

        public static string GetDatabasePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "NGEOS_Desktop");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "ngeos_desktop.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var dbPath = GetDatabasePath();
                optionsBuilder.UseSqlite($"Data Source={dbPath};Cache=Shared;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Donation>().HasIndex(d => d.TransactionId).IsUnique();
            modelBuilder.Entity<Donation>().HasIndex(d => d.ReceiptNumber).IsUnique();
            modelBuilder.Entity<Donation>().HasIndex(d => d.DonationDate);
            modelBuilder.Entity<Cause>().HasIndex(c => c.Category);
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.Timestamp);
            modelBuilder.Entity<SyncQueueItem>().HasIndex(s => s.OperationId).IsUnique();
            modelBuilder.Entity<SyncQueueItem>().HasIndex(s => s.SyncStatus);
        }

        public async Task InitializeAndSeedAsync(IPasswordHasher passwordHasher)
        {
            await Database.EnsureCreatedAsync();

            // Enable WAL mode & foreign keys for high performance SQLite
            try
            {
                await Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
                await Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
            }
            catch { }

            // Seed Master Super Admin if missing
            if (!await Users.AnyAsync())
            {
                var (adminHash, adminSalt) = passwordHasher.HashPassword("Admin@12");
                var admin = new User
                {
                    FullName = "Super Administrator",
                    Email = "admin@ngeos.org",
                    PasswordHash = adminHash,
                    PasswordSalt = adminSalt,
                    Profession = "NGO Executive Director",
                    Organization = "NGEOS Central Secretariat",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var (donorHash, donorSalt) = passwordHasher.HashPassword("User@123");
                var donor = new User
                {
                    FullName = "Syed Saim (Donor)",
                    Email = "saim@ngeos.org",
                    PasswordHash = donorHash,
                    PasswordSalt = donorSalt,
                    Profession = "Software Engineer",
                    Organization = "Tech for Good",
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await Users.AddRangeAsync(admin, donor);

                // Seed Realistic Causes
                var causes = new[]
                {
                    new Cause
                    {
                        Id = 1,
                        Title = "Universal Education for Out-of-School Children",
                        Description = "Providing full schooling, uniforms, books, and digital literacy labs to 500+ underprivileged children.",
                        Category = "Education",
                        TargetAmount = 750000m,
                        RaisedAmount = 520000m,
                        BeneficiariesCount = 520,
                        IsActive = true,
                        IsFeatured = true
                    },
                    new Cause
                    {
                        Id = 2,
                        Title = "Solar Water Purification Plants & Clean Wells",
                        Description = "Installing automated deep-solar water pumping stations across arid drought-affected communities.",
                        Category = "Water Relief",
                        TargetAmount = 1200000m,
                        RaisedAmount = 980000m,
                        BeneficiariesCount = 3400,
                        IsActive = true,
                        IsFeatured = true
                    },
                    new Cause
                    {
                        Id = 3,
                        Title = "Emergency Cardiac Care & Diagnostic Medicine",
                        Description = "Subsidizing life-saving treatments, dialysis, and emergency medications for impoverished patients.",
                        Category = "Healthcare",
                        TargetAmount = 1500000m,
                        RaisedAmount = 1145000m,
                        BeneficiariesCount = 850,
                        IsActive = true,
                        IsFeatured = true
                    },
                    new Cause
                    {
                        Id = 4,
                        Title = "Women Livelihood & Vocational Training Centers",
                        Description = "Micro-grants, sewing machines, and digital marketing training for female-led households.",
                        Category = "Livelihood",
                        TargetAmount = 500000m,
                        RaisedAmount = 390000m,
                        BeneficiariesCount = 280,
                        IsActive = true
                    }
                };

                await Causes.AddRangeAsync(causes);

                // Seed Programmes
                var programmes = new[]
                {
                    new Programme
                    {
                        Id = 1,
                        Title = "Youth Digital Literacy Bootcamp 2026",
                        Description = "Hands-on software skills, graphic design, and freelance mentorship for underprivileged youth.",
                        Category = "Education",
                        Location = "Karachi & Lahore Centers",
                        TargetBeneficiaries = 350,
                        StartDate = DateTime.UtcNow.AddDays(-10),
                        IsActive = true
                    },
                    new Programme
                    {
                        Id = 2,
                        Title = "Mobile Health Clinic & Diagnostic Camps",
                        Description = "Free medical checkups, eye tests, and essential pharmaceuticals in rural districts.",
                        Category = "Healthcare",
                        Location = "Rural Communities",
                        TargetBeneficiaries = 2000,
                        StartDate = DateTime.UtcNow.AddDays(-20),
                        IsActive = true
                    },
                    new Programme
                    {
                        Id = 3,
                        Title = "Winter Warmth & Ration Relief Drive",
                        Description = "Distributing food packages, warm blankets, and thermal clothing to displaced families.",
                        Category = "Relief",
                        Location = "Northern Districts",
                        TargetBeneficiaries = 1200,
                        StartDate = DateTime.UtcNow.AddDays(-35),
                        IsActive = true
                    }
                };

                await Programmes.AddRangeAsync(programmes);

                // Seed Donations
                var donations = new[]
                {
                    new Donation
                    {
                        Id = 1,
                        TransactionId = "NGEOS-TXN-9001",
                        DonorName = "Fatima Tariq",
                        DonorEmail = "fatima.t@example.com",
                        DonorPhone = "03001234567",
                        Amount = 75000m,
                        CauseId = 1,
                        CauseTitle = "Universal Education",
                        PaymentMethod = "Online Card / PayPak",
                        PaymentStatus = "Completed",
                        DonationDate = DateTime.UtcNow.AddDays(-3),
                        ReceiptNumber = "RCP-2026-9001",
                        Notes = "Annual education sponsorship"
                    },
                    new Donation
                    {
                        Id = 2,
                        TransactionId = "NGEOS-TXN-9002",
                        DonorName = "Zubair Ahmed",
                        DonorEmail = "zubair.a@example.com",
                        Amount = 150000m,
                        CauseId = 2,
                        CauseTitle = "Solar Water Purification",
                        PaymentMethod = "JazzCash Mobile Account",
                        PaymentStatus = "Completed",
                        DonationDate = DateTime.UtcNow.AddDays(-2),
                        ReceiptNumber = "RCP-2026-9002",
                        Notes = "Clean water solar well contribution"
                    },
                    new Donation
                    {
                        Id = 3,
                        TransactionId = "NGEOS-TXN-9003",
                        DonorName = "Ayesha Khan",
                        DonorEmail = "ayesha.k@example.com",
                        Amount = 50000m,
                        CauseId = 3,
                        CauseTitle = "Emergency Cardiac Care",
                        PaymentMethod = "Easypaisa Wallet",
                        PaymentStatus = "Completed",
                        DonationDate = DateTime.UtcNow.AddDays(-1),
                        ReceiptNumber = "RCP-2026-9003"
                    },
                    new Donation
                    {
                        Id = 4,
                        TransactionId = "NGEOS-TXN-9004",
                        DonorName = "Bilal Siddiqui",
                        DonorEmail = "bilal.s@example.com",
                        Amount = 100000m,
                        CauseId = 4,
                        CauseTitle = "Women Livelihood Centers",
                        PaymentMethod = "Bank Transfer",
                        PaymentStatus = "Completed",
                        DonationDate = DateTime.UtcNow.AddHours(-6),
                        ReceiptNumber = "RCP-2026-9004",
                        Notes = "Vocational machines grant"
                    }
                };

                await Donations.AddRangeAsync(donations);

                // Seed Queries
                var queries = new[]
                {
                    new Query
                    {
                        Id = 1,
                        FullName = "Hassan Raza",
                        Email = "hassan.r@example.com",
                        Subject = "Corporate matching gift partnership",
                        Message = "Our company is looking to match employee donations 1:1 for the clean water project. Please advise how to proceed.",
                        Status = "Replied",
                        AdminReply = "Thank you Hassan! Our Director of Partnerships has been assigned and will connect with your CSR team.",
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        RepliedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    new Query
                    {
                        Id = 2,
                        FullName = "Maryam Noor",
                        Email = "maryam.n@example.com",
                        Subject = "Volunteer application for Health Camps",
                        Message = "I am a 4th-year medical student interested in volunteering for the mobile clinic drive next weekend.",
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    }
                };

                await Queries.AddRangeAsync(queries);

                // Seed Initial AuditLog
                await AuditLogs.AddAsync(new AuditLog
                {
                    UserEmail = "SYSTEM",
                    Action = "SystemInit",
                    EntityName = "Database",
                    EntityId = "ngeos_desktop.db",
                    Details = "NGEOS Desktop database initialized with master schema and seeded records.",
                    Timestamp = DateTime.UtcNow
                });

                await SaveChangesAsync();
            }
        }
    }
}
