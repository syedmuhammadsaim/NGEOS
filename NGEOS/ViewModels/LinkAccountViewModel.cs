using System.ComponentModel.DataAnnotations;

namespace NGEOS.ViewModels
{
    public class LinkAccountViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? FullName { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? ReturnUrl { get; set; }

        [Required(ErrorMessage = "Please enter your NGEOS account password to verify and link your Google account.")]
        [DataType(DataType.Password)]
        [Display(Name = "Account Password")]
        public string Password { get; set; } = string.Empty;
    }
}
