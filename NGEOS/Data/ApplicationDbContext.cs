using Microsoft.EntityFrameworkCore;
using NGEOS.Models;

namespace NGEOS.Data
{
    public class ApplicationDbContext : DbContext
    {
        // Token table for password‑reset flow
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<NGO> NGOs => Set<NGO>();
        public DbSet<Cause> Causes => Set<Cause>();
        public DbSet<Donation> Donations => Set<Donation>();
        public DbSet<Partner> Partners => Set<Partner>();
        public DbSet<Programme> Programmes => Set<Programme>();
        public DbSet<ProgrammeInterest> ProgrammeInterests => Set<ProgrammeInterest>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<GalleryItem> GalleryItems => Set<GalleryItem>();
        public DbSet<AboutPage> AboutPages => Set<AboutPage>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<JobOpportunity> JobOpportunities => Set<JobOpportunity>();
        public DbSet<FAQ> FAQs => Set<FAQ>();
        public DbSet<UserQuery> UserQueries => Set<UserQuery>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
        public DbSet<FriendInvitation> FriendInvitations => Set<FriendInvitation>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

        // Autonomous AI Ecosystem Tables
        public DbSet<AgentConfiguration> AgentConfigurations => Set<AgentConfiguration>();
        public DbSet<AgentReport> AgentReports => Set<AgentReport>();
        public DbSet<AgentSecurityEvent> AgentSecurityEvents => Set<AgentSecurityEvent>();
        public DbSet<AgentUserRestriction> AgentUserRestrictions => Set<AgentUserRestriction>();
        public DbSet<AgentAppeal> AgentAppeals => Set<AgentAppeal>();
        public DbSet<AgentConversation> AgentConversations => Set<AgentConversation>();
        public DbSet<AgentMessage> AgentMessages => Set<AgentMessage>();
        public DbSet<AgentAutomationJob> AgentAutomationJobs => Set<AgentAutomationJob>();
        public DbSet<AgentUsageStatistic> AgentUsageStatistics => Set<AgentUsageStatistic>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Email Unique Index
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.GoogleId);

            // NGO Relationships
            modelBuilder.Entity<Cause>()
                .HasOne(c => c.Ngo)
                .WithMany(n => n.Causes)
                .HasForeignKey(c => c.NgoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Programme>()
                .HasOne(p => p.Ngo)
                .WithMany(n => n.Programmes)
                .HasForeignKey(p => p.NgoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Donation Relationships
            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Cause)
                .WithMany(c => c.Donations)
                .HasForeignKey(d => d.CauseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.User)
                .WithMany(u => u.Donations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Programme)
                .WithMany()
                .HasForeignKey(d => d.ProgrammeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Payment Transaction Relationships & Indexes
            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(pt => pt.Donation)
                .WithMany(d => d.Transactions)
                .HasForeignKey(pt => pt.DonationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(pt => pt.InternalReference)
                .IsUnique();

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(pt => new { pt.GatewayName, pt.GatewayReference });

            // Programme Interest Relationships
            modelBuilder.Entity<ProgrammeInterest>()
                .HasOne(pi => pi.Programme)
                .WithMany(p => p.Interests)
                .HasForeignKey(pi => pi.ProgrammeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProgrammeInterest>()
                .HasOne(pi => pi.User)
                .WithMany(u => u.ProgrammeInterests)
                .HasForeignKey(pi => pi.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // UserQuery Relationships
            modelBuilder.Entity<UserQuery>()
                .HasOne(q => q.User)
                .WithMany(u => u.Queries)
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // FriendInvitation Relationships
            modelBuilder.Entity<FriendInvitation>()
                .HasOne(fi => fi.User)
                .WithMany(u => u.SentInvitations)
                .HasForeignKey(fi => fi.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FriendInvitation>()
                .HasOne(fi => fi.Cause)
                .WithMany(c => c.FriendInvitations)
                .HasForeignKey(fi => fi.CauseId)
                .OnDelete(DeleteBehavior.SetNull);

            // AboutPage Slug Unique Index
            modelBuilder.Entity<AboutPage>()
                .HasIndex(a => a.Slug)
                .IsUnique();

            // PasswordResetToken Relationships
            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(t => t.Code);

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(t => t.ResetSessionToken);

            // Autonomous Agent Indexes & Relationships
            modelBuilder.Entity<AgentReport>()
                .HasIndex(r => r.ReportCode)
                .IsUnique();

            modelBuilder.Entity<AgentReport>()
                .HasIndex(r => r.Timestamp);

            modelBuilder.Entity<AgentSecurityEvent>()
                .HasIndex(s => s.EventCode)
                .IsUnique();

            modelBuilder.Entity<AgentSecurityEvent>()
                .HasIndex(s => s.DetectedAt);

            modelBuilder.Entity<AgentConversation>()
                .HasIndex(c => c.SessionId);

            modelBuilder.Entity<AgentAutomationJob>()
                .HasIndex(j => j.ExecutionId)
                .IsUnique();
        }
    }
}
