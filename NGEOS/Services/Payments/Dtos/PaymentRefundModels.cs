namespace NGEOS.Services.Payments.Dtos
{
    public class PaymentRefundRequest
    {
        public int DonationId { get; set; }
        public string InternalReference { get; set; } = string.Empty;
        public string GatewayReference { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public decimal Amount { get => RefundAmount; set => RefundAmount = value; }
        public string Currency { get; set; } = "PKR";
        public string Reason { get; set; } = "Donor requested refund";
        public string AdminEmail { get; set; } = string.Empty;
    }

    public class PaymentRefundResult
    {
        public bool IsSuccess { get; set; }
        public string? RefundReference { get; set; }
        public decimal RefundedAmount { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawResponse { get; set; }
    }

    public class GatewayHealthResult
    {
        public string GatewayName { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public bool IsHealthy { get; set; }
        public string Environment { get; set; } = "Sandbox";
        public string StatusMessage { get; set; } = string.Empty; // "Sandbox Ready", "Credentials Missing", "Connection Failed", "Production Ready"
        public string Message => StatusMessage;
        public string? PingLatencyMs { get; set; }
    }
}
