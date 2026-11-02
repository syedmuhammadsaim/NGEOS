using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGEOS.Models
{
    public class Programme
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public int? NgoId { get; set; }
        [ForeignKey("NgoId")]
        public virtual NGO? Ngo { get; set; }

        // Categories: "Education", "Health Care", "Privileged children (Specially challenged)", "Women Empowerment", "Elderly Support"
        [Required, MaxLength(100)]
        public string Category { get; set; } = "Education";

        [Required, MaxLength(350)]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        public string FullDescription { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required, MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        public int TargetBeneficiaries { get; set; } = 500;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<ProgrammeInterest> Interests { get; set; } = new List<ProgrammeInterest>();
    }
}
