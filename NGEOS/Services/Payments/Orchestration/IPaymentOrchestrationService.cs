using System.Threading;
using System.Threading.Tasks;
using NGEOS.Models;
using NGEOS.Services.Payments.Dtos;
using NGEOS.ViewModels;

namespace NGEOS.Services.Payments.Orchestration
{
    public interface IPaymentOrchestrationService
    {
        Task<PaymentInitiateResult> StartDonationPaymentAsync(
            DonateRequestViewModel model, 
            int? userId, 
            string clientIp, 
            string baseUrl, 
            CancellationToken cancellationToken = default);
    }
}
