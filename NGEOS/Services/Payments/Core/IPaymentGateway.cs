using System.Threading;
using System.Threading.Tasks;
using NGEOS.Services.Payments.Dtos;

namespace NGEOS.Services.Payments.Core
{
    /// <summary>
    /// Common contract for all payment gateway integrations (Safepay, JazzCash, Easypaisa, PayFast).
    /// </summary>
    public interface IPaymentGateway
    {
        string GatewayName { get; }
        bool IsConfigured { get; }
        string EnvironmentName { get; }
        PaymentGatewayCapabilities Capabilities { get; }

        Task<PaymentInitiateResult> InitiatePaymentAsync(PaymentInitiateRequest request, CancellationToken cancellationToken = default);

        Task<PaymentVerifyResult> VerifyPaymentAsync(PaymentCallbackData callbackData, CancellationToken cancellationToken = default);

        Task<PaymentRefundResult> ProcessRefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken = default);

        Task<GatewayHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    }
}
