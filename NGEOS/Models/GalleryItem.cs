using System.ComponentModel.DataAnnotations;

namespace NGEOS.Models
{
    public class GalleryItem
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        // Categories: "Education", "Healthcare", "Children", "Relief", "Events", "Community"
        [Required, MaxLength(50)]
        public string Category { get; set; } = "Education";

        [Required, MaxLength(250)]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(350)]
        public string? Description { get; set; }

        public DateTime? EventDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
