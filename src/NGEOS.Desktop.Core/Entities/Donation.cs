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
    }
}
