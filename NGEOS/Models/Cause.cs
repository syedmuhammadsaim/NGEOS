using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGEOS.Models
{
    public class Cause
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(350)]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        public string FullDescription { get; set; } = string.Empty;

        // Categories: Children, Education, Healthcare, Disabled Persons, Women Welfare, Youth Development, Elderly Care
        [Required, MaxLength(50)]
        public string Category { get; set; } = "Children";

        [Column(TypeName = "decimal(18,2)")]
        [Range(1, 100000000)]
        public decimal TargetAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RaisedAmount { get; set; } = 0;

        [Required, MaxLength(300)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsFeatured { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public int? NgoId { get; set; }
        [ForeignKey("NgoId")]
        public virtual NGO? Ngo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Computed percentage helper
        [NotMapped]
        public int PercentageRaised => TargetAmount > 0 ? (int)Math.Min(100, Math.Round((RaisedAmount / TargetAmount) * 100)) : 0;

        // Navigation
        public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public virtual ICollection<FriendInvitation> FriendInvitations { get; set; } = new List<FriendInvitation>();
    }
}
