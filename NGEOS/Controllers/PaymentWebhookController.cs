using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NGEOS.Services.Payments.Orchestration;

namespace NGEOS.Controllers
{
    [ApiController]
    [Route("api/payments/webhooks")]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly IPaymentWebhookService _webhookService;
        private readonly ILogger<PaymentWebhookController> _logger;

        public PaymentWebhookController(
            IPaymentWebhookService webhookService,
            ILogger<PaymentWebhookController> logger)
        {
            _webhookService = webhookService;
            _logger = logger;
        }

        [HttpPost("{gateway}")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> HandleWebhook(string gateway, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received webhook event for gateway: {Gateway}", gateway);

            try
            {
                var result = await _webhookService.HandleWebhookAsync(gateway, Request, cancellationToken);

                if (result.IsSuccessful)
                {
                    _logger.LogInformation("Webhook handled successfully for Reference: {Ref}", result.InternalReference);
                    return Ok(new
                    {
                        success = true,
                        reference = result.InternalReference,
                        status = result.Status,
                        message = result.Message
                    });
                }

                _logger.LogWarning("Webhook verification not successful for Gateway: {Gateway}. Reason: {Msg}",
                    gateway, result.Message);

                return Ok(new
                {
                    success = false,
                    reference = result.InternalReference,
                    status = result.Status,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while handling payment webhook for {Gateway}", gateway);
                // Return 200 or 400 depending on provider to prevent retry storms if unrecoverable
                return StatusCode(500, new { success = false, error = "Webhook processing error" });
            }
        }
    }
}
