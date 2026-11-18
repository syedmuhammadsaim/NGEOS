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
        public DbSet<ProgrammeInterest> ProgrammeInterests => Set<ProgrammeInterest>();
        public DbSet<Query> Queries => Set<Query>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
        public DbSet<NGO> NGOs => Set<NGO>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<GalleryItem> GalleryItems => Set<GalleryItem>();
        public DbSet<FAQ> FAQs => Set<FAQ>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<Partner> Partners => Set<Partner>();
        public DbSet<JobOpportunity> JobOpportunities => Set<JobOpportunity>();
        public DbSet<FriendInvitation> FriendInvitations => Set<FriendInvitation>();
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
                optionsBuilder.UseSqlite($"Data Source={dbPath};");
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
            modelBuilder.Entity<Cause>().Ignore(c => c.ProgressPercentage);
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.Timestamp);
            modelBuilder.Entity<SyncQueueItem>().HasIndex(s => s.OperationId).IsUnique();
            modelBuilder.Entity<SyncQueueItem>().HasIndex(s => s.SyncStatus);
            modelBuilder.Entity<NGO>().HasIndex(n => n.RegistrationNumber);
            modelBuilder.Entity<FAQ>().HasIndex(f => f.Category);
            modelBuilder.Entity<Event>().HasIndex(e => e.EventDate);
        }

        private static void DbLog(string msg)
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var folder = Path.Combine(appData, "NGEOS_Desktop");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                var file = Path.Combine(folder, "app_startup.log");
                File.AppendAllText(file, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [DB] {msg}\n");
            }
            catch { }
        }

        public async Task InitializeAndSeedAsync(IPasswordHasher passwordHasher)
        {
            DbLog("Entering InitializeAndSeedAsync...");
            await Database.EnsureCreatedAsync();
            DbLog("EnsureCreatedAsync completed.");

            // Enable WAL mode & foreign keys for high performance SQLite
            try
            {
                DbLog("Setting PRAGMA WAL & foreign keys...");
                await Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
                await Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
                DbLog("PRAGMA configured.");
            }
            catch (Exception ex)
            {
                DbLog($"PRAGMA warning: {ex.Message}");
            }

            // Safe auto-migration for hybrid sync columns if database existed previously
            try { await Database.ExecuteSqlRawAsync("ALTER TABLE Causes ADD COLUMN IsSynced INTEGER NOT NULL DEFAULT 0;"); } catch { }
            try { await Database.ExecuteSqlRawAsync("ALTER TABLE Causes ADD COLUMN ServerId INTEGER NULL;"); } catch { }
            try { await Database.ExecuteSqlRawAsync("ALTER TABLE Causes ADD COLUMN LastModifiedUtc TEXT NOT NULL DEFAULT '2026-01-01';"); } catch { }
            try { await Database.ExecuteSqlRawAsync("ALTER TABLE Donations ADD COLUMN IsSynced INTEGER NOT NULL DEFAULT 0;"); } catch { }
            try { await Database.ExecuteSqlRawAsync("ALTER TABLE Donations ADD COLUMN ServerId INTEGER NULL;"); } catch { }
            try { await Database.ExecuteSqlRawAsync("ALTER TABLE Donations ADD COLUMN LastModifiedUtc TEXT NOT NULL DEFAULT '2026-01-01';"); } catch { }
            try { await Database.ExecuteSqlRawAsync("ALTER TABLE Donations ADD COLUMN ClientGuid TEXT NULL;"); } catch { }

            // Ensure all new website-parity tables exist in local SQLite (Safe DDL Migration)
            try
            {
                await Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS NGOs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    RegistrationNumber TEXT NOT NULL,
    ShortDescription TEXT NOT NULL,
    FullDescription TEXT NOT NULL,
    LogoUrl TEXT NOT NULL,
    FocusArea TEXT NOT NULL,
    Website TEXT NULL,
    Email TEXT NOT NULL,
    Phone TEXT NOT NULL,
    Address TEXT NULL,
    EstablishedYear INTEGER NOT NULL DEFAULT 2018,
    IsVerified INTEGER NOT NULL DEFAULT 1,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Events (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    ShortDescription TEXT NOT NULL,
    FullDescription TEXT NOT NULL,
    EventDate TEXT NOT NULL,
    Location TEXT NOT NULL,
    Organizer TEXT NOT NULL,
    IsUpcoming INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS GalleryItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Category TEXT NOT NULL,
    Description TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS FAQs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Question TEXT NOT NULL,
    Answer TEXT NOT NULL,
    Category TEXT NOT NULL,
    DisplayOrder INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS TeamMembers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Role TEXT NOT NULL,
    Bio TEXT NOT NULL,
    Email TEXT NOT NULL,
    DisplayOrder INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Partners (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Category TEXT NOT NULL,
    WebsiteUrl TEXT NULL,
    Description TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS JobOpportunities (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Department TEXT NOT NULL,
    JobType TEXT NOT NULL,
    Location TEXT NOT NULL,
    Description TEXT NOT NULL,
    Requirements TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    PostedDate TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ProgrammeInterests (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProgrammeId INTEGER NOT NULL,
    UserId INTEGER NULL,
    FullName TEXT NOT NULL,
    Email TEXT NOT NULL,
    Phone TEXT NOT NULL,
    Occupation TEXT NULL,
    InterestType TEXT NOT NULL,
    Message TEXT NULL,
    Status TEXT NOT NULL DEFAULT 'Pending',
    SubmittedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ContactMessages (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Email TEXT NOT NULL,
    Phone TEXT NULL,
    Subject TEXT NOT NULL,
    Message TEXT NOT NULL,
    IsRead INTEGER NOT NULL DEFAULT 0,
    ReplyNotes TEXT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Queries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NULL,
    FullName TEXT NOT NULL,
    Email TEXT NOT NULL,
    Phone TEXT NULL,
    Subject TEXT NOT NULL,
    Message TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Pending',
    AdminReply TEXT NULL,
    CreatedAt TEXT NOT NULL,
    RepliedAt TEXT NULL
);

CREATE TABLE IF NOT EXISTS FriendInvitations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SenderUserId INTEGER NOT NULL,
    FriendName TEXT NOT NULL,
    FriendEmail TEXT NOT NULL,
    InvitationMessage TEXT NOT NULL,
    SentAt TEXT NOT NULL
);
");
                DbLog("Safe DDL CREATE TABLE IF NOT EXISTS succeeded.");
            }
            catch (Exception ex)
            {
                DbLog($"Safe DDL error: {ex.Message}");
            }

            // 1. Seed Master Super Admin and Regular User if missing
            DbLog("1. Checking Users...");
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
                    FullName = "Syed Saim (Citizen Donor)",
                    Email = "saim@ngeos.org",
                    PasswordHash = donorHash,
                    PasswordSalt = donorSalt,
                    Profession = "Software Architect",
                    Organization = "Global Tech for Good",
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await Users.AddRangeAsync(admin, donor);
                await SaveChangesAsync();
            }

            // 2. Seed Realistic Causes if missing
            DbLog("2. Checking Causes...");
            if (!await Causes.AnyAsync())
            {
                var causes = new[]
                {
                    new Cause
                    {
                        Title = "Universal Education for Out-of-School Children",
                        Description = "Providing full schooling, uniforms, books, and digital literacy labs to 500+ underprivileged children across underserved districts.",
                        Category = "Education",
                        TargetAmount = 750000m,
                        RaisedAmount = 520000m,
                        BeneficiariesCount = 520,
                        IsActive = true,
                        IsFeatured = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Cause
                    {
                        Title = "Solar Water Purification Plants & Deep Clean Wells",
                        Description = "Installing automated deep-solar water pumping stations and filtration plants across arid drought-affected communities.",
                        Category = "Water Relief",
                        TargetAmount = 1200000m,
                        RaisedAmount = 980000m,
                        BeneficiariesCount = 3400,
                        IsActive = true,
                        IsFeatured = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Cause
                    {
                        Title = "Emergency Cardiac Care & Diagnostic Medicine Subsidies",
                        Description = "Subsidizing life-saving treatments, kidney dialysis, and emergency medications for impoverished families.",
                        Category = "Healthcare",
                        TargetAmount = 1500000m,
                        RaisedAmount = 1145000m,
                        BeneficiariesCount = 850,
                        IsActive = true,
                        IsFeatured = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Cause
                    {
                        Title = "Women Livelihood & Vocational Training Centers",
                        Description = "Providing micro-grants, industrial sewing equipment, and digital freelancing skill training for female-led households.",
                        Category = "Livelihood",
                        TargetAmount = 500000m,
                        RaisedAmount = 390000m,
                        BeneficiariesCount = 280,
                        IsActive = true,
                        IsFeatured = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Cause
                    {
                        Title = "Orphan Care & Nutritious Living Sponsorship",
                        Description = "Holistic monthly foster care, nutrition packages, medical coverage, and emotional support for orphaned children.",
                        Category = "Orphan Care",
                        TargetAmount = 800000m,
                        RaisedAmount = 610000m,
                        BeneficiariesCount = 420,
                        IsActive = true,
                        IsFeatured = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await Causes.AddRangeAsync(causes);
                await SaveChangesAsync();
            }

            // 3. Seed Programmes if missing
            if (!await Programmes.AnyAsync())
            {
                var programmes = new[]
                {
                    new Programme
                    {
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
                        Title = "Winter Warmth & Ration Relief Drive",
                        Description = "Distributing food packages, warm blankets, and thermal clothing to displaced families.",
                        Category = "Relief",
                        Location = "Northern Districts",
                        TargetBeneficiaries = 1200,
                        StartDate = DateTime.UtcNow.AddDays(-35),
                        IsActive = true
                    },
                    new Programme
                    {
                        Title = "Clean Water Community Well Operations",
                        Description = "Routine maintenance, water quality testing, and solar maintenance of community wells.",
                        Category = "Water Relief",
                        Location = "Tharparkar & Balochistan",
                        TargetBeneficiaries = 5000,
                        StartDate = DateTime.UtcNow.AddDays(-5),
                        IsActive = true
                    }
                };

                await Programmes.AddRangeAsync(programmes);
                await SaveChangesAsync();
            }

            // 4. Seed Donations if missing
            if (!await Donations.AnyAsync())
            {
                var firstCause = await Causes.FirstOrDefaultAsync();
                int cId = firstCause?.Id ?? 1;

                var donations = new[]
                {
                    new Donation
                    {
                        TransactionId = "NGEOS-TXN-9001",
                        DonorName = "Fatima Tariq",
                        DonorEmail = "fatima.t@example.com",
                        DonorPhone = "03001234567",
                        Amount = 75000m,
                        CauseId = cId,
                        CauseTitle = "Universal Education",
                        PaymentMethod = "Online Card / PayPak",
                        PaymentStatus = "Completed",
                        DonationDate = DateTime.UtcNow.AddDays(-3),
                        ReceiptNumber = "RCP-2026-9001",
                        Notes = "Annual education sponsorship"
                    },
                    new Donation
                    {
                        TransactionId = "NGEOS-TXN-9002",
                        DonorName = "Zubair Ahmed",
                        DonorEmail = "zubair.a@example.com",
                        Amount = 150000m,
                        CauseId = cId,
                        CauseTitle = "Solar Water Purification",
                        PaymentMethod = "JazzCash Mobile Account",
                        PaymentStatus = "Completed",
                        DonationDate = DateTime.UtcNow.AddDays(-2),
                        ReceiptNumber = "RCP-2026-9002",
                        Notes = "Clean water solar well contribution"
                    },
                    new Donation
                    {
                        TransactionId = "NGEOS-TXN-9003",
                        DonorName = "Ayesha Khan",
                        DonorEmail = "ayesha.k@example.com",
                        Amount = 50000m,
                        CauseId = cId,
                        CauseTitle = "Emergency Cardiac Care",
                        PaymentMethod = "Easypaisa Wallet",
                        PaymentStatus = "Completed",
                        DonationDate = DateTime.UtcNow.AddDays(-1),
                        ReceiptNumber = "RCP-2026-9003"
                    },
                    new Donation
                    {
                        TransactionId = "NGEOS-TXN-9004",
                        DonorName = "Bilal Siddiqui",
                        DonorEmail = "bilal.s@example.com",
                        Amount = 100000m,
                        CauseId = cId,
                        CauseTitle = "Women Livelihood Centers",
                        PaymentMethod = "Bank Transfer",
                        PaymentStatus = "Completed",
                        DonationDate = DateTime.UtcNow.AddHours(-6),
                        ReceiptNumber = "RCP-2026-9004",
                        Notes = "Vocational machines grant"
                    }
                };

                await Donations.AddRangeAsync(donations);
                await SaveChangesAsync();
            }

            // 5. Seed Accredited NGOs if missing
            if (!await NGOs.AnyAsync())
            {
                var ngos = new[]
                {
                    new NGO
                    {
                        Name = "Hope & Dignity Welfare Trust",
                        RegistrationNumber = "NGO-PK-2019-8812",
                        ShortDescription = "Leading non-profit focused on emergency disaster relief, child education, and clean drinking water.",
                        FullDescription = "Established in 2018, Hope & Dignity Welfare Trust works directly in rural districts providing emergency disaster relief, scholarships, and solar water stations.",
                        FocusArea = "Disaster Relief & Education",
                        Email = "info@hopedignity.org",
                        Phone = "+92 21 34567890",
                        Address = "Plot 45-C, Shahrah-e-Faisal, Karachi",
                        EstablishedYear = 2018,
                        IsVerified = true,
                        IsActive = true
                    },
                    new NGO
                    {
                        Name = "Al-Shifa Healthcare Foundation",
                        RegistrationNumber = "NGO-PK-2020-4109",
                        ShortDescription = "Providing subsidized cardiac surgeries, dialysis care, and free mobile clinics.",
                        FullDescription = "Al-Shifa Foundation has operated across 14 rural districts offering medical screenings, subsidizing expensive medications, and performing surgeries for low-income patients.",
                        FocusArea = "Healthcare & Medicine",
                        Email = "contact@alshifa-foundation.org",
                        Phone = "+92 51 2233445",
                        Address = "Sector F-8/2, Islamabad",
                        EstablishedYear = 2020,
                        IsVerified = true,
                        IsActive = true
                    },
                    new NGO
                    {
                        Name = "Qalam Education & Literacy Network",
                        RegistrationNumber = "NGO-PK-2017-1044",
                        ShortDescription = "Reintegrating street children and out-of-school girls into formal accredited schooling.",
                        FullDescription = "Qalam Education Network runs 12 community schools and 4 mobile computer laboratories providing accredited curriculum, books, and nutritious meals.",
                        FocusArea = "Primary & Vocational Education",
                        Email = "partnerships@qalamedu.org",
                        Phone = "+92 42 35789012",
                        Address = "Gulberg III, Lahore",
                        EstablishedYear = 2017,
                        IsVerified = true,
                        IsActive = true
                    }
                };

                await NGOs.AddRangeAsync(ngos);
                await SaveChangesAsync();
            }

            // 6. Seed Events if missing
            if (!await Events.AnyAsync())
            {
                var events = new[]
                {
                    new Event
                    {
                        Title = "Annual Global Humanitarian Gala & Donor Summit 2026",
                        ShortDescription = "Honoring accredited NGO partners, major benefactors, and reviewing audited welfare impact.",
                        FullDescription = "Join international donors, accredited NGO directors, and community leaders for our annual transparency conference and charity dinner.",
                        EventDate = DateTime.UtcNow.AddDays(14),
                        Location = "Grand Ballroom, Marriott Hotel, Islamabad",
                        Organizer = "NGEOS Central Secretariat",
                        IsUpcoming = true
                    },
                    new Event
                    {
                        Title = "Emergency Flood Relief & Food Ration Packing Drive",
                        ShortDescription = "Volunteer gathering to pack 5,000 family food ration bags for rural drought-stricken villages.",
                        FullDescription = "Volunteers of all ages are welcome to join our logistics warehouse team to prepare essential nutrition, flour, oil, and emergency hygiene kits.",
                        EventDate = DateTime.UtcNow.AddDays(5),
                        Location = "NGEOS Logistics Hub, Karachi",
                        Organizer = "Volunteer Task Force",
                        IsUpcoming = true
                    },
                    new Event
                    {
                        Title = "Free Pediatric & Eye Care Medical Camp",
                        ShortDescription = "Mobile doctors diagnosing 1,000+ children and dispensing free glasses and medicines.",
                        FullDescription = "Volunteer pediatricians and ophthalmologists conducting full diagnostic screening camps for underprivileged school students.",
                        EventDate = DateTime.UtcNow.AddDays(21),
                        Location = "Community Centre, District Badin",
                        Organizer = "Al-Shifa Healthcare Alliance",
                        IsUpcoming = true
                    }
                };

                await Events.AddRangeAsync(events);
                await SaveChangesAsync();
            }

            // 7. Seed FAQs if missing
            if (!await FAQs.AnyAsync())
            {
                var faqs = new[]
                {
                    new FAQ
                    {
                        Question = "How does NGEOS ensure 100% donation transparency?",
                        Answer = "Every single donation received is assigned a unique cryptographic transaction ID, registered in our immutable ledger, and tracked until field disbursement. Zero administrative deductions are taken from Zakat contributions.",
                        Category = "Donations & Payment",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new FAQ
                    {
                        Question = "Can I donate while offline in the desktop application?",
                        Answer = "Yes! NGEOS Desktop features a high-performance SQLite WAL local database. You can record offline donations or cash receipts instantly. When internet connectivity is restored, the two-way sync engine reconciles records with cloud servers safely.",
                        Category = "General",
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new FAQ
                    {
                        Question = "How is Zakat calculated on the platform?",
                        Answer = "Our Zakat Calculator evaluates your qualifying assets (cash, gold, silver, investments, inventory) minus immediate liabilities against the contemporary Nisab threshold (87.48g gold or 612.36g silver), calculating the mandatory 2.5% rate.",
                        Category = "Tax & Receipts",
                        DisplayOrder = 3,
                        IsActive = true
                    },
                    new FAQ
                    {
                        Question = "How can an NGO get verified on NGEOS?",
                        Answer = "NGOs undergo rigorous auditing including verification of official government registration charters, tax exemption certificates, bank account audits, and on-ground field evaluation before receiving the Verified Accredited badge.",
                        Category = "NGO Partnership",
                        DisplayOrder = 4,
                        IsActive = true
                    }
                };

                await FAQs.AddRangeAsync(faqs);
                await SaveChangesAsync();
            }

            // 8. Seed Team Members if missing
            if (!await TeamMembers.AnyAsync())
            {
                var team = new[]
                {
                    new TeamMember
                    {
                        Name = "Dr. Tariq Mansoor",
                        Role = "Executive Chairman",
                        Bio = "Over 25 years of humanitarian leadership across international welfare agencies and public health initiatives.",
                        Email = "tariq.m@ngeos.org",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new TeamMember
                    {
                        Name = "Sara Al-Hashimi",
                        Role = "Director of Global Operations",
                        Bio = "Specializes in emergency logistics supply chains, rapid relief deployment, and multilateral NGO partnerships.",
                        Email = "sara.h@ngeos.org",
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new TeamMember
                    {
                        Name = "Syed Saim",
                        Role = "Chief Software Architect",
                        Bio = "Architect behind NGEOS 100% native offline-first desktop systems, cloud synchronization, and financial transparency engines.",
                        Email = "saim@ngeos.org",
                        DisplayOrder = 3,
                        IsActive = true
                    }
                };

                await TeamMembers.AddRangeAsync(team);
                await SaveChangesAsync();
            }

            // 9. Seed Corporate Partners if missing
            if (!await Partners.AnyAsync())
            {
                var partners = new[]
                {
                    new Partner
                    {
                        Name = "Apex Global Cloud Solutions",
                        Category = "Technology Partner",
                        WebsiteUrl = "https://apexcloud.example.com",
                        Description = "Providing cloud infrastructure grants and enterprise security auditing for NGEOS.",
                        IsActive = true
                    },
                    new Partner
                    {
                        Name = "Habib Bank Corporate CSR",
                        Category = "Corporate Sponsor",
                        WebsiteUrl = "https://hbl.example.com",
                        Description = "Matching 1:1 corporate employee donations for youth digital literacy bootcamp.",
                        IsActive = true
                    },
                    new Partner
                    {
                        Name = "National Pharma Logistics",
                        Category = "Healthcare Partner",
                        WebsiteUrl = "https://pharmalog.example.com",
                        Description = "Donating subsidized essential medicines and vaccine cold-storage transit.",
                        IsActive = true
                    }
                };

                await Partners.AddRangeAsync(partners);
                await SaveChangesAsync();
            }

            // 10. Seed Gallery Items if missing
            if (!await GalleryItems.AnyAsync())
            {
                var gallery = new[]
                {
                    new GalleryItem { Title = "Solar Water Plant Opening in Tharparkar", Category = "Relief", Description = "Inaugurating clean drinking water plant providing 10,000 gallons daily." },
                    new GalleryItem { Title = "Digital Lab Induction Ceremony", Category = "Education", Description = "50 students receiving high-performance laptops and coding kits." },
                    new GalleryItem { Title = "Emergency Medical Diagnostic Camp", Category = "Healthcare", Description = "Screening 600 rural residents for vision, cardiac, and diabetic ailments." },
                    new GalleryItem { Title = "Winter Ration Distribution Convoy", Category = "Relief", Description = "Loading relief trucks with thermal blankets and essential food supplies." }
                };

                await GalleryItems.AddRangeAsync(gallery);
                await SaveChangesAsync();
            }

            // 11. Seed Queries if missing
            if (!await Queries.AnyAsync())
            {
                var queries = new[]
                {
                    new Query
                    {
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
                        FullName = "Maryam Noor",
                        Email = "maryam.n@example.com",
                        Subject = "Volunteer application for Health Camps",
                        Message = "I am a 4th-year medical student interested in volunteering for the mobile clinic drive next weekend.",
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    }
                };

                await Queries.AddRangeAsync(queries);
                await SaveChangesAsync();
            }

            // 12. Seed Initial AuditLog if missing
            if (!await AuditLogs.AnyAsync())
            {
                await AuditLogs.AddAsync(new AuditLog
                {
                    UserEmail = "SYSTEM",
                    Action = "SystemInit",
                    EntityName = "Database",
                    EntityId = "ngeos_desktop.db",
                    Details = "NGEOS Desktop database initialized with full website-parity master schema and seeded records.",
                    Timestamp = DateTime.UtcNow
                });

                await SaveChangesAsync();
            }

            DbLog("All 12 seeds verified and saved successfully. Database fully ready.");
        }
    }
}
