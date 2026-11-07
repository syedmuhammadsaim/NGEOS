using System;

namespace NGEOS.Services.Payments.Core
{
    /// <summary>
    /// Metadata describing capabilities and supported features of a payment gateway.
    /// </summary>
    public class PaymentGatewayCapabilities
    {
        public bool SupportsHostedCheckout { get; set; } = true;
        public bool SupportsDirectApi { get; set; } = false;
        public bool SupportsWebhooks { get; set; } = true;
        public bool SupportsAutomatedRefunds { get; set; } = true;
        public bool SupportsPartialRefunds { get; set; } = false;
        public string[] SupportedCurrencies { get; set; } = new[] { "PKR" };
        public string[] SupportedPaymentMethods { get; set; } = Array.Empty<string>();
    }
}
