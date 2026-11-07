using System.IO;
using Microsoft.EntityFrameworkCore;
using NGEOS.Desktop.Models;

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
    }
}
