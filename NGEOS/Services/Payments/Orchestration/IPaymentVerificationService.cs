using System.Threading;
using System.Threading.Tasks;
using NGEOS.Models;
using NGEOS.Services.Payments.Dtos;

namespace NGEOS.Services.Payments.Orchestration
{
    public interface IPaymentVerificationService
    {
        Task<PaymentVerifyResult> ProcessCallbackVerificationAsync(
            string gatewayName, 
            PaymentCallbackData callbackData, 
            CancellationToken cancellationToken = default);

        Task<PaymentRefundResult> RequestRefundAsync(
            int donationId, 
            decimal refundAmount, 
            string reason, 
            string adminEmail, 
            CancellationToken cancellationToken = default);
    }
}
