using System.Collections.Generic;

namespace NGEOS.Services.Payments.Dtos
{
    public enum PaymentActionType
    {
        Redirect,
        PostForm,
        DirectSuccess
    }

    public class PaymentInitiateResult
    {
        public bool IsSuccess { get; set; }
        public string GatewayName { get; set; } = string.Empty;
        public string? InternalReference { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PKR";
        public string? RedirectUrl { get; set; }
        public string? GatewayReference { get; set; } // Provider Token / OrderId / Tracking ID
        public bool RequiresAutoSubmitForm { get; set; }
        public string? FormActionUrl { get; set; }
        public string? PostEndpoint { get => FormActionUrl; set => FormActionUrl = value; }
        public string FormMethod { get; set; } = "POST";
        public Dictionary<string, string> FormFields { get; set; } = new();
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawPayload { get; set; }

        public PaymentActionType ActionType => RequiresAutoSubmitForm ? PaymentActionType.PostForm :
            (!string.IsNullOrEmpty(RedirectUrl) ? PaymentActionType.Redirect : PaymentActionType.DirectSuccess);
    }
}
