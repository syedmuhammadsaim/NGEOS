using System;

namespace NGEOS.Services.Payments.Configuration
{
    public class PaymentOptions
    {
        public const string SectionName = "Payments";

        public string GlobalEnvironment { get; set; } = "Sandbox"; // "Sandbox", "Production"
        public string DefaultCurrency { get; set; } = "PKR";
        public int TimeoutSeconds { get; set; } = 20;
        public bool SimulationMode { get; set; } = true;

        public SafepayOptions Safepay { get; set; } = new();
        public JazzCashOptions JazzCash { get; set; } = new();
        public EasypaisaOptions Easypaisa { get; set; } = new();
        public PayFastOptions PayFast { get; set; } = new();
    }

    public class SafepayOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string Environment { get; set; } = "Sandbox";

        public string BaseUrl => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "https://api.getsafepay.com"
            : "https://sandbox.api.getsafepay.com";

        public string CheckoutBaseUrl => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "https://getsafepay.com/checkout/pay"
            : "https://sandbox.api.getsafepay.com/checkout/pay";

        public bool IsSimulation => IsConfigured && (ApiKey.Contains("sample") || ApiKey.Contains("test_key") || ApiKey.StartsWith("sec_sandbox_test"));
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) &&
                                    !string.IsNullOrWhiteSpace(SecretKey) &&
                                    !ApiKey.Contains("YOUR_") &&
                                    !ApiKey.Contains("your_");
    }

    public class JazzCashOptions
    {
        public string MerchantId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string IntegritySalt { get; set; } = string.Empty;
        public string Environment { get; set; } = "Sandbox";
        public string ReturnUrl { get; set; } = string.Empty;

        public string CheckoutUrl => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "https://payments.jazzcash.com.pk/CustomerPortal/transactionmanagement/merchantform/"
            : "https://sandbox.jazzcash.com.pk/CustomerPortal/transactionmanagement/merchantform/";

        public string InquiryUrl => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "https://payments.jazzcash.com.pk/ApplicationAPI/API/2.0/Purchase/DoInquiry"
            : "https://sandbox.jazzcash.com.pk/ApplicationAPI/API/2.0/Purchase/DoInquiry";

        public string RefundUrl => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "https://payments.jazzcash.com.pk/ApplicationAPI/API/2.0/Purchase/DoRefund"
            : "https://sandbox.jazzcash.com.pk/ApplicationAPI/API/2.0/Purchase/DoRefund";

        public bool IsSimulation => IsConfigured && (MerchantId == "MC12345" || Password == "test_password" || IntegritySalt.Contains("test_salt"));
        public bool IsConfigured => !string.IsNullOrWhiteSpace(MerchantId) &&
                                    !string.IsNullOrWhiteSpace(Password) &&
                                    !string.IsNullOrWhiteSpace(IntegritySalt) &&
                                    !MerchantId.Contains("YOUR_") &&
                                    !MerchantId.Contains("your_");
    }

    public class EasypaisaOptions
    {
        public string StoreId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Environment { get; set; } = "Sandbox";
        public string PostBackUrl { get; set; } = string.Empty;

        public string CheckoutUrl => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "https://easypay.easypaisa.com.pk/easypay/Index.jsf"
            : "https://easypaystg.easypaisa.com.pk/easypay/Index.jsf";

        public string InquiryUrl => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "https://easypay.easypaisa.com.pk/easypay-service/rest/v4/inquire-transaction"
            : "https://easypaystg.easypaisa.com.pk/easypay-service/rest/v4/inquire-transaction";

        public string RefundUrl => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "https://easypay.easypaisa.com.pk/easypay-service/rest/v4/refund-transaction"
            : "https://easypaystg.easypaisa.com.pk/easypay-service/rest/v4/refund-transaction";

        public bool IsSimulation => IsConfigured && (StoreId == "1234" || SecretKey.Contains("test_secret"));
        public bool IsConfigured => !string.IsNullOrWhiteSpace(StoreId) &&
                                    !StoreId.Contains("YOUR_") &&
                                    !StoreId.Contains("your_");
    }

    public class PayFastOptions
    {
        public string MerchantId { get; set; } = string.Empty;
        public string SecuredKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string Environment { get; set; } = "Sandbox";

        public string TokenUrl => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "https://ipg.apps.net.pk/Ecommerce/api/Transaction/GetAccessToken"
            : "https://ipguat.apps.net.pk/Ecommerce/api/Transaction/GetAccessToken";

        public string CheckoutUrl => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "https://ipg.apps.net.pk/Ecommerce/api/Transaction/v1"
            : "https://ipguat.apps.net.pk/Ecommerce/api/Transaction/v1";

        public string InquiryUrl => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "https://ipg.apps.net.pk/Ecommerce/api/Transaction/TransactionStatus"
            : "https://ipguat.apps.net.pk/Ecommerce/api/Transaction/TransactionStatus";

        public bool IsSimulation => IsConfigured && (MerchantId == "1001" || SecuredKey.Contains("test_secured"));
        public bool IsConfigured => !string.IsNullOrWhiteSpace(MerchantId) &&
                                    !string.IsNullOrWhiteSpace(SecuredKey) &&
                                    !MerchantId.Contains("YOUR_") &&
                                    !MerchantId.Contains("your_");
    }
}
