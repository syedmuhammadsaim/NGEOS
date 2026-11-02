using System.ComponentModel.DataAnnotations;

namespace NGEOS.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(25)]
        public string? Phone { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        // Professional Data
        [MaxLength(100)]
        public string? Profession { get; set; }

        [MaxLength(150)]
        public string? Organization { get; set; }

        [Required, MaxLength(20)]
        public string Role { get; set; } = "User"; // "Admin" or "User"

        [MaxLength(250)]
        public string? ProfileImageUrl { get; set; }

        [MaxLength(100)]
        public string? GoogleId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public virtual ICollection<ProgrammeInterest> ProgrammeInterests { get; set; } = new List<ProgrammeInterest>();
        public virtual ICollection<UserQuery> Queries { get; set; } = new List<UserQuery>();
        public virtual ICollection<FriendInvitation> SentInvitations { get; set; } = new List<FriendInvitation>();
    }
}
