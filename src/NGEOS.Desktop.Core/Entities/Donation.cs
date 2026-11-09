using System;

namespace NGEOS.Desktop.Core.Entities
{
    public class Donation
    {
        public int Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string DonorName { get; set; } = string.Empty;
        public string DonorEmail { get; set; } = string.Empty;
        public string? DonorPhone { get; set; }
        public decimal Amount { get; set; }
        public int CauseId { get; set; }
        public string CauseTitle { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Direct Bank / Cash";
        public string PaymentStatus { get; set; } = "Completed"; // Completed, Pending, Refunded
        public DateTime DonationDate { get; set; } = DateTime.UtcNow;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string? Notes { get; set; }

        // Hybrid Sync Tracking
        public bool IsSynced { get; set; } = false;
        public int? ServerId { get; set; }
        public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
        public string ClientGuid { get; set; } = Guid.NewGuid().ToString("N");
    }
}
