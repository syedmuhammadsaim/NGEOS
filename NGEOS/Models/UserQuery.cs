using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGEOS.Models
{
    public class UserQuery
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        // Categories: "General Inquiry", "Donation Assistance", "NGO Partnership", "Volunteering", "Technical Issue"
        [Required, MaxLength(100)]
        public string Category { get; set; } = "General Inquiry";

        [Required]
        public string Message { get; set; } = string.Empty;

        public string? AdminReply { get; set; }

        // Status: "Pending", "In Progress", "Replied", "Closed"
        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RepliedAt { get; set; }
    }
}
