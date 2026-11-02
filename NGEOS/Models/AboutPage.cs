using System.ComponentModel.DataAnnotations;

namespace NGEOS.Models
{
    public class AboutPage
    {
        public int Id { get; set; }

        // Slugs: "what-we-do", "our-mission", "our-team", "career-with-us", "our-achievements", "our-supporters", "read-about-us"
        [Required, MaxLength(60)]
        public string Slug { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Subtitle { get; set; }

        [Required]
        public string ContentHtml { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? BannerImageUrl { get; set; }

        [MaxLength(250)]
        public string? FeaturedImageUrl { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
