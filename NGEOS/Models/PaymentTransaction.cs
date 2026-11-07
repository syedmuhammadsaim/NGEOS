using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGEOS.Models
{
    /// <summary>
    /// Audit-proof financial transaction ledger tracking real-time gateway requests,
    /// cryptographic signatures, callbacks, webhooks, and refund lifecycle.
    /// </summary>
    public class PaymentTransaction
    {
        public int Id { get; set; }

        public int DonationId { get; set; }
        [ForeignKey("DonationId")]
        public virtual Donation? Donation { get; set; }

        [Required, MaxLength(50)]
        public string GatewayName { get; set; } = "Safepay"; // "Safepay", "JazzCash", "Easypaisa", "PayFast"

        [Required, MaxLength(100)]
        public string InternalReference { get; set; } = string.Empty; // e.g. "GA-TXN-202611-ABC123"

        [MaxLength(150)]
        public string? GatewayReference { get; set; } // Provider Tracker Token / OrderId / RefNum

        [Column(TypeName = "decimal(18,2)")]
        [Range(1, 100000000)]
        public decimal Amount { get; set; }

        [Required, MaxLength(10)]
        public string Currency { get; set; } = "PKR";

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending"; // "Initiated", "Pending", "Processing", "Completed", "Failed", "Cancelled", "Refunded"

        [Required, MaxLength(20)]
        public string Environment { get; set; } = "Sandbox"; // "Sandbox", "Production"

        [Required, MaxLength(30)]
        public string VerificationStatus { get; set; } = "Pending"; // "Pending", "Verified", "Mismatch", "Failed"

        [MaxLength(150)]
        public string? IdempotencyKey { get; set; }

        public string? RawCallbackPayload { get; set; }

        public string? RawWebhookPayload { get; set; }

        [MaxLength(50)]
        public string? ErrorCode { get; set; }

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RefundAmount { get; set; }

        [MaxLength(100)]
        public string? RefundReference { get; set; }

        [MaxLength(300)]
        public string? RefundReason { get; set; }

        public DateTime? RefundedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }
    }
}
