using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NGEOS.Services.Payments.Dtos;

namespace NGEOS.Services.Payments.Orchestration
{
    public class PaymentWebhookService : IPaymentWebhookService
    {
        private readonly IPaymentVerificationService _verificationService;
        private readonly ILogger<PaymentWebhookService> _logger;

        public PaymentWebhookService(
            IPaymentVerificationService verificationService,
            ILogger<PaymentWebhookService> logger)
        {
            _verificationService = verificationService;
            _logger = logger;
        }

        public async Task<PaymentVerifyResult> HandleWebhookAsync(
            string gatewayName,
            HttpRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Incoming payment webhook received for gateway: {Gateway}", gatewayName);

            // Read raw body
            string rawBody = "";
            request.EnableBuffering();
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync(cancellationToken);
                request.Body.Position = 0;
            }

            var callbackData = new PaymentCallbackData
            {
                GatewayName = gatewayName,
                RawBody = rawBody
            };

            // Parse Query parameters
            foreach (var q in request.Query)
            {
                callbackData.QueryParameters[q.Key] = q.Value.ToString();
            }

            // Parse Headers
            foreach (var h in request.Headers)
            {
                callbackData.Headers[h.Key] = h.Value.ToString();
            }

            // Parse Form fields if form-urlencoded
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync(cancellationToken);
                foreach (var f in form)
                {
                    callbackData.FormFields[f.Key] = f.Value.ToString();
                }
            }

            // Extract references from query or form
            if (callbackData.QueryParameters.TryGetValue("ref", out var qRef)) callbackData.InternalReference = qRef;
            if (callbackData.FormFields.TryGetValue("pp_BillReference", out var jcRef)) callbackData.InternalReference = jcRef;
            if (callbackData.FormFields.TryGetValue("orderRefNum", out var epRef)) callbackData.InternalReference = epRef;
            if (callbackData.FormFields.TryGetValue("BASKET_ID", out var pfRef)) callbackData.InternalReference = pfRef;
            if (callbackData.QueryParameters.TryGetValue("tracker", out var sfRef)) callbackData.GatewayReference = sfRef;

            return await _verificationService.ProcessCallbackVerificationAsync(gatewayName, callbackData, cancellationToken);
        }
    }
}
