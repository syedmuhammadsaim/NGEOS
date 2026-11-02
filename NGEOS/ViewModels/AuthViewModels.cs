using System.ComponentModel.DataAnnotations;

namespace NGEOS.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Display(Name = "Residential / Mailing Address")]
        public string? Address { get; set; }

        // Professional Data
        [Required(ErrorMessage = "Profession / Occupation is required.")]
        [Display(Name = "Profession / Occupation")]
        [StringLength(100)]
        public string Profession { get; set; } = string.Empty;

        [Required(ErrorMessage = "Organization / Company Name is required.")]
        [Display(Name = "Organization / Employer / Institute")]
        [StringLength(150)]
        public string Organization { get; set; } = string.Empty;

        [Display(Name = "I agree to the Terms of Service & Privacy Policy")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the Terms of Service.")]
        public bool AgreeToTerms { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email address or phone number is required.")]
        [Display(Name = "Email or Phone Number")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me on this device")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
