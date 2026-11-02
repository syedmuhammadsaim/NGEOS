using System.ComponentModel.DataAnnotations;

namespace NGEOS.ViewModels
{
    public class VerifyCodeViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please provide a valid email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the 6-digit verification code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be exactly 6 digits.")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "Verification code must contain 6 numbers only.")]
        [Display(Name = "6-Digit Verification Code")]
        public string Code { get; set; } = string.Empty;
    }
}
