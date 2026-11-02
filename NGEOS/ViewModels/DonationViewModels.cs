using System.ComponentModel.DataAnnotations;
using NGEOS.Models;

namespace NGEOS.ViewModels
{
    public class DonateRequestViewModel
    {
        [Required(ErrorMessage = "Please select a cause to support.")]
        [Display(Name = "Select Cause")]
        public int CauseId { get; set; }

        [Required]
        [Display(Name = "Contribution Type")]
        public string DonationType { get; set; } = "General"; // "General" or "Zakat"

        [Required(ErrorMessage = "Please specify a donation amount.")]
        [Range(1, 1000000, ErrorMessage = "Donation amount must be between $1 and $1,000,000.")]
        [Display(Name = "Donation Amount ($)")]
        public decimal Amount { get; set; } = 50;

        [Required(ErrorMessage = "Your full name is required.")]
        [Display(Name = "Your Full Name")]
        [StringLength(100, MinimumLength = 2)]
        public string DonorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Your email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address (for Receipt)")]
        public string DonorEmail { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number (Optional)")]
        public string? DonorPhone { get; set; }

        // Dummy Payment Card Details with Validation
        [Required(ErrorMessage = "Card Type is required.")]
        [Display(Name = "Payment Method")]
        public string CardType { get; set; } = "Credit Card";

        [Required(ErrorMessage = "Cardholder Name is required.")]
        [Display(Name = "Name on Card")]
        [StringLength(100, MinimumLength = 3)]
        public string CardHolderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "16-digit Card Number is required.")]
        [RegularExpression(@"^(?:\d[ -]*?){13,19}$", ErrorMessage = "Please enter a valid card number (13-19 digits).")]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry Date is required.")]
        [RegularExpression(@"^[0-9]{1,2}\/?([0-9]{2,4})$", ErrorMessage = "Expiry format must be MM/YY or MM/YYYY.")]
        [Display(Name = "Expiry Date (MM/YY)")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVV Security Code is required.")]
        [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
        [Display(Name = "CVV / CVC")]
        public string CVV { get; set; } = string.Empty;

        [Display(Name = "Leave an Encouraging Note (Optional)")]
        [StringLength(500)]
        public string? Notes { get; set; }

        public List<Cause>? AvailableCauses { get; set; }
    }

    public class DonationReceiptViewModel
    {
        public string TransactionId { get; set; } = string.Empty;
        public string CauseTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string NgoName { get; set; } = string.Empty;
        public string DonorName { get; set; } = string.Empty;
        public string DonorEmail { get; set; } = string.Empty;
        public string? DonorPhone { get; set; }
        public decimal Amount { get; set; }
        public string CardType { get; set; } = string.Empty;
        public string MaskedCardNumber { get; set; } = string.Empty;
        public DateTime DonationDate { get; set; }
        public string PaymentStatus { get; set; } = "Completed";
        public string DonationType { get; set; } = "General";
        public bool IsReceiptEmailSent { get; set; }
        public string? Notes { get; set; }
    }
}
