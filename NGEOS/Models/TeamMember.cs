using System.ComponentModel.DataAnnotations;

namespace NGEOS.Models
{
    public class TeamMember
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Role { get; set; } = string.Empty;

        [Required, MaxLength(350)]
        public string Bio { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string PhotoUrl { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(200)]
        public string? LinkedInUrl { get; set; }

        [MaxLength(200)]
        public string? TwitterUrl { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
