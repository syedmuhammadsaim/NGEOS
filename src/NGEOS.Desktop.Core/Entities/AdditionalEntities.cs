using System;
using System.Collections.Generic;

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

    public class ProgrammeInterest
    {
        public int Id { get; set; }
        public int ProgrammeId { get; set; }
        public int? UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Occupation { get; set; }
        public string InterestType { get; set; } = "Volunteer"; // Volunteer, Participant, Sponsor
        public string? Message { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Contacted
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    public class Query
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
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

    public class ContactMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public string? ReplyNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class NGO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string FocusArea { get; set; } = "Humanitarian Relief";
        public string? Website { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int EstablishedYear { get; set; } = 2018;
        public bool IsVerified { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public DateTime EventDate { get; set; } = DateTime.UtcNow.AddDays(7);
        public string Location { get; set; } = string.Empty;
        public string Organizer { get; set; } = "NGEOS Central Platform";
        public bool IsUpcoming { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class GalleryItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = "Relief"; // Education, Healthcare, Relief, Community
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class FAQ
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = "General"; // General, Donations & Payment, Tax & Receipts, Programmes & Volunteering
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public class TeamMember
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public class Partner
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Corporate Sponsor";
        public string? WebsiteUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class JobOpportunity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = "Field Operations";
        public string JobType { get; set; } = "Full Time";
        public string Location { get; set; } = "Headquarters / Remote";
        public string Description { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime PostedDate { get; set; } = DateTime.UtcNow;
    }

    public class FriendInvitation
    {
        public int Id { get; set; }
        public int SenderUserId { get; set; }
        public string FriendName { get; set; } = string.Empty;
        public string FriendEmail { get; set; } = string.Empty;
        public string InvitationMessage { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
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
