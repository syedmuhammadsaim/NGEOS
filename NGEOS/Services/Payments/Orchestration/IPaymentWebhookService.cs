using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NGEOS.Services.Payments.Dtos;

namespace NGEOS.Services.Payments.Orchestration
{
    public interface IPaymentWebhookService
    {
        Task<PaymentVerifyResult> HandleWebhookAsync(
            string gatewayName, 
            HttpRequest request, 
            CancellationToken cancellationToken = default);
    }
}
