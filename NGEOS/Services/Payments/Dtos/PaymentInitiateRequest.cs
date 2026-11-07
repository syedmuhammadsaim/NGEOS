using System;
using System.Collections.Generic;

namespace NGEOS.Services.Payments.Dtos
{
    public class PaymentInitiateRequest
    {
        public int DonationId { get; set; }
        public string InternalReference { get; set; } = string.Empty; // e.g. "GA-TXN-202611-ABC123"
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PKR";
        public string DonationType { get; set; } = "General"; // "General" or "Zakat"
        public string ItemTitle { get; set; } = "NGEOS Charity Donation";
        public string ItemDescription { get; set; } = string.Empty;
        public string DonorName { get; set; } = string.Empty;
        public string DonorEmail { get; set; } = string.Empty;
        public string? DonorPhone { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
        public string ClientIpAddress { get; set; } = "127.0.0.1";
        public Dictionary<string, string> CustomMetadata { get; set; } = new();
    }
}
