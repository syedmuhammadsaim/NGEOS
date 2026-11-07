using System.ComponentModel.DataAnnotations;
using NGEOS.Models;

namespace NGEOS.ViewModels
{
    public class UserDashboardViewModel
    {
        public User User { get; set; } = null!;
        public decimal TotalDonated { get; set; }
        public int CausesSupportedCount { get; set; }
        public int ProgrammesJoinedCount { get; set; }
        public int QueriesCount { get; set; }

        public List<Donation> RecentDonations { get; set; } = new();
        public List<ProgrammeInterest> MyProgrammeInterests { get; set; } = new();
        public List<UserQuery> MyQueries { get; set; } = new();
        public List<FriendInvitation> MyInvitations { get; set; } = new();
    }

    public class InviteFriendViewModel
    {
        [Required(ErrorMessage = "Your Name is required.")]
        [Display(Name = "Your Name")]
        public string SenderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Your Email is required.")]
        [EmailAddress]
        [Display(Name = "Your Email Address")]
        public string SenderEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Friend's Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid friend email address.")]
        [Display(Name = "Friend's Email Address")]
        public string FriendEmail { get; set; } = string.Empty;

        [Display(Name = "Select Cause to Share (Optional)")]
        public int? CauseId { get; set; }

        [Required(ErrorMessage = "Please enter a short personal invite message.")]
        [StringLength(600)]
        [Display(Name = "Personal Invitation Message")]
        public string PersonalMessage { get; set; } = "Hey! I've been supporting NGEOS and wanted to invite you to join this wonderful mission to help people in need. Check out their impactful causes!";

        public List<Cause>? Causes { get; set; }
    }

    public class ProgrammeInterestRequestViewModel
    {
        [Required]
        public int ProgrammeId { get; set; }

        public string? ProgrammeTitle { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Your Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone]
        [Display(Name = "Contact Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Current Occupation / Background")]
        public string? Occupation { get; set; }

        [Required(ErrorMessage = "Please select your interest role.")]
        [Display(Name = "How would you like to participate?")]
        public string InterestType { get; set; } = "Volunteer"; // "Volunteer", "Participant", "Sponsor", "Observer"

        [Display(Name = "Additional Notes or Relevant Experience (Optional)")]
        [StringLength(500)]
        public string? Message { get; set; }
    }

    public class RaiseQueryRequestViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Your Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress]
        [Display(Name = "Your Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required.")]
        [Display(Name = "Query Subject")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        [Display(Name = "Query Category")]
        public string Category { get; set; } = "General Inquiry";

        [Required(ErrorMessage = "Please provide details in your query message.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 2000 characters.")]
        [Display(Name = "Query Description / Questions")]
        public string Message { get; set; } = string.Empty;
    }

    public class ContactUsViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [Display(Name = "Your Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number (Optional)")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [Display(Name = "Message")]
        [StringLength(2000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalDonationsCount { get; set; }
        public decimal TotalDonationsAmount { get; set; }
        public int TotalCauses { get; set; }
        public int TotalNgos { get; set; }
        public int TotalProgrammes { get; set; }
        public int TotalEvents { get; set; }
        public int PendingQueriesCount { get; set; }
        public int TotalVolunteersCount { get; set; }
        public int UnreadMessagesCount { get; set; }

        public List<Donation> RecentDonations { get; set; } = new();
        public List<UserQuery> RecentQueries { get; set; } = new();
        public List<User> RecentUsers { get; set; } = new();
        public List<ProgrammeInterest> RecentProgrammeInterests { get; set; } = new();
        public List<ContactMessage> RecentMessages { get; set; } = new();
    }

    public class AdminReplyQueryViewModel
    {
        public int QueryId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string OriginalMessage { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        [Required(ErrorMessage = "Reply message cannot be empty.")]
        [Display(Name = "Admin Response / Resolution")]
        public string AdminReply { get; set; } = string.Empty;

        [Display(Name = "Update Status")]
        public string Status { get; set; } = "Replied";
    }
}
