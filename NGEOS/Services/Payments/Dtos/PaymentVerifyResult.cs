using System;

namespace NGEOS.Services.Payments.Dtos
{
    public class PaymentVerifyResult
    {
        public bool IsVerified { get; set; }
        public bool IsPaymentSuccessful { get; set; }
        public bool IsSuccessful => IsPaymentSuccessful && IsVerified;
        public string Status { get; set; } = "Pending"; // "Completed", "Pending", "Failed", "Cancelled"
        public string GatewayName { get; set; } = string.Empty;
        public string? InternalReference { get; set; }
        public string? GatewayReference { get; set; }
        public decimal? VerifiedAmount { get; set; }
        public string? VerifiedCurrency { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string Message => ErrorMessage ?? (IsSuccessful ? "Transaction completed successfully." : Status);
        public string? RawResponse { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
