using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGEOS.Models
{
    public class FriendInvitation
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, MaxLength(100)]
        public string SenderName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string SenderEmail { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string FriendEmail { get; set; } = string.Empty;

        public int? CauseId { get; set; }
        [ForeignKey("CauseId")]
        public virtual Cause? Cause { get; set; }

        [MaxLength(600)]
        public string? PersonalMessage { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
