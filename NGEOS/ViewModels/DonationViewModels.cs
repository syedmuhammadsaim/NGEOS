using System.ComponentModel.DataAnnotations;
using NGEOS.Models;

namespace NGEOS.ViewModels
{
    public class DonateRequestViewModel
    {
        [Required(ErrorMessage = "Please select a cause to support.")]
        [Display(Name = "Select Cause")]
        public int CauseId { get; set; }

        public int? ProgrammeId { get; set; }

        [Required]
        [Display(Name = "Contribution Type")]
        public string DonationType { get; set; } = "General"; // "General" or "Zakat"

        [Required(ErrorMessage = "Please specify a donation amount.")]
        [Range(1, 10000000, ErrorMessage = "Donation amount must be between 1 and 10,000,000.")]
        [Display(Name = "Donation Amount")]
        public decimal Amount { get; set; } = 1000;

        [Required(ErrorMessage = "Please select a payment gateway.")]
        [Display(Name = "Payment Gateway")]
        public string PaymentGateway { get; set; } = "Safepay"; // Safepay, JazzCash, Easypaisa, PayFast

        [Display(Name = "Currency")]
        public string Currency { get; set; } = "PKR";

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

        // Optional legacy card fields (for compatibility)
        public string? CardType { get; set; } = "Online Gateway";
        public string? CardHolderName { get; set; }
        public string? CardNumber { get; set; }
        public string? ExpiryDate { get; set; }
        public string? CVV { get; set; }

        [Display(Name = "Leave an Encouraging Note (Optional)")]
        [StringLength(500)]
        public string? Notes { get; set; }

        public List<Cause>? AvailableCauses { get; set; }
    }

    public class DonationReceiptViewModel
    {
        public string TransactionId { get; set; } = string.Empty;
        public string GatewayReference { get; set; } = string.Empty;
        public string PaymentGateway { get; set; } = "Safepay";
        public string Currency { get; set; } = "PKR";
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
        public string CleanNotes
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Notes)) return string.Empty;
                return System.Text.RegularExpressions.Regex.Replace(Notes, @"\[(Type|Email|Gateway):[^\]]+\]\s*", "").Trim();
            }
        }
    }
}
