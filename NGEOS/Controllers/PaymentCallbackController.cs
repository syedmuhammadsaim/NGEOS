using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NGEOS.Services.Payments.Dtos;
using NGEOS.Services.Payments.Orchestration;

namespace NGEOS.Controllers
{
    [Route("payments/callback")]
    public class PaymentCallbackController : Controller
    {
        private readonly IPaymentVerificationService _verificationService;
        private readonly ILogger<PaymentCallbackController> _logger;

        public PaymentCallbackController(
            IPaymentVerificationService verificationService,
            ILogger<PaymentCallbackController> logger)
        {
            _verificationService = verificationService;
            _logger = logger;
        }

        [HttpGet("{gateway}")]
        [HttpPost("{gateway}")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Callback(string gateway, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Payment callback received for gateway: {Gateway} via {Method}", gateway, Request.Method);

            var callbackData = new PaymentCallbackData
            {
                GatewayName = gateway
            };

            // Read Query parameters
            foreach (var q in Request.Query)
            {
                callbackData.QueryParams[q.Key] = q.Value.ToString();
            }

            // Read Form data if POST
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(cancellationToken);
                foreach (var f in form)
                {
                    callbackData.FormData[f.Key] = f.Value.ToString();
                }
            }

            // Extract InternalReference and GatewayReference safely
            if (string.IsNullOrEmpty(callbackData.InternalReference))
            {
                if (callbackData.QueryParams.TryGetValue("ref", out var rVal))
                    callbackData.InternalReference = rVal;
                else if (callbackData.FormData.TryGetValue("ref", out var rfVal))
                    callbackData.InternalReference = rfVal;
                else if (callbackData.FormData.TryGetValue("pp_TxnRefNo", out var jcRef))
                    callbackData.GatewayReference = jcRef;
                else if (callbackData.FormData.TryGetValue("orderRefNumber", out var epRef))
                    callbackData.InternalReference = epRef;
                else if (callbackData.FormData.TryGetValue("basket_id", out var pfRef))
                    callbackData.InternalReference = pfRef;
            }
            if (string.IsNullOrEmpty(callbackData.GatewayReference))
            {
                if (callbackData.QueryParams.TryGetValue("gatewayRef", out var grVal))
                    callbackData.GatewayReference = grVal;
                else if (callbackData.FormData.TryGetValue("gatewayRef", out var grfVal))
                    callbackData.GatewayReference = grfVal;
                else if (callbackData.QueryParams.TryGetValue("tracker", out var sfTracker))
                    callbackData.GatewayReference = sfTracker;
            }

            // Process callback verification through centralized orchestration
            var verifyResult = await _verificationService.ProcessCallbackVerificationAsync(
                gateway,
                callbackData,
                cancellationToken);

            if (verifyResult.IsSuccessful)
            {
                _logger.LogInformation("Callback verification succeeded for Reference: {Ref}", verifyResult.InternalReference);
                return RedirectToAction("Confirmation", "Donate", new { transactionId = verifyResult.InternalReference });
            }

            _logger.LogWarning("Callback verification failed for Reference: {Ref}. Message: {Msg}",
                verifyResult.InternalReference, verifyResult.Message);

            return RedirectToAction("PaymentResult", "Donate", new
            {
                status = verifyResult.Status,
                reference = verifyResult.InternalReference,
                gateway = gateway,
                message = verifyResult.Message
            });
        }
    }
}
