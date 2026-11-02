using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGEOS.Models
{
    public class NGO
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        public string FullDescription { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string LogoUrl { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? BannerUrl { get; set; }

        [Required, MaxLength(100)]
        public string FocusArea { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Website { get; set; }

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Address { get; set; }

        public int EstablishedYear { get; set; } = 2018;

        public bool IsVerified { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Cause> Causes { get; set; } = new List<Cause>();
        public virtual ICollection<Programme> Programmes { get; set; } = new List<Programme>();
    }
}
