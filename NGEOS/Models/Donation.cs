using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGEOS.Models
{
    public class Donation
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string TransactionId { get; set; } = string.Empty; // e.g. "GA-TXN-984214"

        [Required]
        public int CauseId { get; set; }
        [ForeignKey("CauseId")]
        public virtual Cause? Cause { get; set; }

        public int? ProgrammeId { get; set; }
        [ForeignKey("ProgrammeId")]
        public virtual Programme? Programme { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, MaxLength(100)]
        public string DonorName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string DonorEmail { get; set; } = string.Empty;

        [MaxLength(25)]
        public string? DonorPhone { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(1, 1000000)]
        public decimal Amount { get; set; }

        // Dummy Payment Details
        [Required, MaxLength(30)]
        public string CardType { get; set; } = "Credit Card"; // "Credit Card", "Debit Card", "Visa", "MasterCard"

        [Required, MaxLength(100)]
        public string CardHolderName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string MaskedCardNumber { get; set; } = string.Empty; // e.g. "**** **** **** 4242"

        [Required, MaxLength(30)]
        public string PaymentStatus { get; set; } = "Completed"; // "Completed", "Pending", "Failed"

        [MaxLength(50)]
        public string PaymentGateway { get; set; } = "Credit Card"; // "Safepay", "JazzCash", "Easypaisa", "PayFast"

        [MaxLength(10)]
        public string Currency { get; set; } = "PKR";

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime DonationDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();

        [NotMapped]
        public string DonationType => (Notes != null && Notes.Contains("[Type:Zakat]", StringComparison.OrdinalIgnoreCase)) ? "Zakat" : "General";

        [NotMapped]
        public bool IsZakat => DonationType == "Zakat";

        [NotMapped]
        public bool IsReceiptEmailSent => Notes != null && Notes.Contains("[Email:Sent]", StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public DateTime PktDate => DonationDate.AddHours(5);

        [NotMapped]
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
