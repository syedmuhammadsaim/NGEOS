using System.ComponentModel.DataAnnotations;

namespace NGEOS.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Registered Email Address")]
        public string Email { get; set; } = string.Empty;
    }
}
