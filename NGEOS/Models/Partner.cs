using System.ComponentModel.DataAnnotations;

namespace NGEOS.Models
{
    public class Partner
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        // Categories: "Corporate Sponsor", "Technology Partner", "Healthcare Partner", "Community Ally"
        [Required, MaxLength(100)]
        public string Category { get; set; } = "Corporate Sponsor";

        [Required, MaxLength(250)]
        public string LogoUrl { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? WebsiteUrl { get; set; }

        [MaxLength(350)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
