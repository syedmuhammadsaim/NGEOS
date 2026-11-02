using System.ComponentModel.DataAnnotations;

namespace NGEOS.Models
{
    public class FAQ
    {
        public int Id { get; set; }

        [Required, MaxLength(250)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        // Categories: "General", "Donations & Payment", "Tax & Receipts", "Programmes & Volunteering", "NGO Partnership"
        [Required, MaxLength(100)]
        public string Category { get; set; } = "General";

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
