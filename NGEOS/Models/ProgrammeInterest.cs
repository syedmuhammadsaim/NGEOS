using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGEOS.Models
{
    public class ProgrammeInterest
    {
        public int Id { get; set; }

        [Required]
        public int ProgrammeId { get; set; }
        [ForeignKey("ProgrammeId")]
        public virtual Programme? Programme { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(25)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Occupation { get; set; }

        // Role: "Volunteer", "Participant", "Sponsor", "Observer"
        [Required, MaxLength(50)]
        public string InterestType { get; set; } = "Volunteer";

        [MaxLength(500)]
        public string? Message { get; set; }

        // Status: "Pending", "Approved", "Contacted", "Completed"
        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
