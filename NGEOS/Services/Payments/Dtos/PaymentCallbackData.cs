using System.Collections.Generic;

namespace NGEOS.Services.Payments.Dtos
{
    public class PaymentCallbackData
    {
        public string GatewayName { get; set; } = string.Empty;
        public string? InternalReference { get; set; }
        public string? GatewayReference { get; set; }
        public string RawBody { get; set; } = string.Empty;
        public Dictionary<string, string> QueryParameters { get; set; } = new();
        public Dictionary<string, string> FormFields { get; set; } = new();
        public Dictionary<string, string> Headers { get; set; } = new();

        public Dictionary<string, string> QueryParams => QueryParameters;
        public Dictionary<string, string> FormData => FormFields;
    }
}
