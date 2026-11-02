using System.ComponentModel.DataAnnotations;

namespace NGEOS.Models
{
    public class JobOpportunity
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Department { get; set; } = "Field Operations";

        // "Full Time", "Part Time", "Volunteer", "Internship"
        [Required, MaxLength(50)]
        public string JobType { get; set; } = "Full Time";

        [Required, MaxLength(100)]
        public string Location { get; set; } = "Headquarters / Remote";

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Requirements { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime PostedDate { get; set; } = DateTime.UtcNow;
    }
}
