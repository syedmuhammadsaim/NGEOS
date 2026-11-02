using System.ComponentModel.DataAnnotations;

namespace NGEOS.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(350)]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        public string FullDescription { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [Required, MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string ImageUrl { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Organizer { get; set; } = "NGEOS Foundation";

        public bool IsUpcoming { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
