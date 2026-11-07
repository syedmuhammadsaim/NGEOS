using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NGEOS.Services.Payments.Configuration;
using NGEOS.Services.Payments.Core;
using NGEOS.Services.Payments.Dtos;

namespace NGEOS.Services.Payments.Gateways
{
    /// <summary>
    /// Easypaisa Pakistan payment gateway integration service.
    /// Implements hosted checkout parameterization, checksum hashing,
    /// callback verification, inquire-transaction server verification, and refund support.
    /// Official Merchant Portal: https://merchantportal.easypaisa.com.pk/
    /// </summary>
    public class EasypaisaPaymentService : IPaymentGateway
    {
        public const string HttpClientName = "EasypaisaClient";

        private readonly HttpClient _httpClient;
        private readonly PaymentOptions _options;
        private readonly ILogger<EasypaisaPaymentService> _logger;

        public string GatewayName => "Easypaisa";
        public bool IsConfigured => _options.Easypaisa.IsConfigured;
        public string EnvironmentName => _options.Easypaisa.Environment;

        public PaymentGatewayCapabilities Capabilities => new()
        {
            SupportsHostedCheckout = true,
            SupportsDirectApi = true,
            SupportsWebhooks = true,
            SupportsAutomatedRefunds = true,
            SupportsPartialRefunds = false,
            SupportedCurrencies = new[] { "PKR" },
            SupportedPaymentMethods = new[] { "Easypaisa Mobile Wallet", "Debit/Credit Card", "OTC Token / Shop" }
        };

        public EasypaisaPaymentService(
            IHttpClientFactory httpClientFactory,
            IOptions<PaymentOptions> options,
            ILogger<EasypaisaPaymentService> logger)
        {
            _httpClient = httpClientFactory.CreateClient(HttpClientName);
            _options = options.Value;
            _logger = logger;
        }

        public Task<PaymentInitiateResult> InitiatePaymentAsync(PaymentInitiateRequest request, CancellationToken cancellationToken = default)
        {
            var result = new PaymentInitiateResult { GatewayName = GatewayName };

            if (!IsConfigured)
            {
                result.IsSuccess = false;
                result.ErrorCode = "CONFIG_MISSING";
                result.ErrorMessage = "Easypaisa merchant store credentials are not configured.";
                return Task.FromResult(result);
            }

            if (_options.SimulationMode || _options.Easypaisa.IsSimulation)
            {
                result.IsSuccess = true;
                result.RequiresAutoSubmitForm = false;
                result.RedirectUrl = $"/payments/simulator?ref={request.InternalReference}&gateway={GatewayName}";
                result.GatewayReference = $"EP-SIM-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
                return Task.FromResult(result);
            }

            try
            {
                // Easypaisa operates in Pakistan Standard Time (PKT = UTC+5)
                var pktNow = DateTime.UtcNow.AddHours(5);
                var expiryDate = pktNow.AddHours(2).ToString("yyyyMMdd HHmmss");
                var amountFormatted = Math.Round(request.Amount, 2).ToString("F2");
                var storeId = _options.Easypaisa.StoreId.Trim();
                var orderRef = request.InternalReference;

                // Clean postBackURL - Easypaisa WAF rejects query strings and localhost loopback
                // Allow custom EASYPAISA_POSTBACK_URL from .env (for Ngrok / live domain in testing)
                var cleanPostBackUrl = !string.IsNullOrWhiteSpace(_options.Easypaisa.PostBackUrl)
                    ? _options.Easypaisa.PostBackUrl.Trim()
                    : request.ReturnUrl;

                if (cleanPostBackUrl.Contains("?"))
                {
                    cleanPostBackUrl = cleanPostBackUrl.Substring(0, cleanPostBackUrl.IndexOf('?'));
                }

                // Form parameters for Easypaisa hosted checkout
                var formFields = new Dictionary<string, string>
                {
                    { "storeId", storeId },
                    { "amount", amountFormatted },
                    { "postBackURL", cleanPostBackUrl },
                    { "orderRefNum", orderRef },
                    { "expiryDate", expiryDate },
                    { "autoRedirect", "1" },
                    { "paymentMethod", "MA_PAYMENT_METHOD" }
                };

                if (!string.IsNullOrWhiteSpace(request.DonorEmail))
                {
                    formFields["emailAddr"] = request.DonorEmail.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.DonorPhone))
                {
                    var cleanPhone = new string(request.DonorPhone.Where(char.IsDigit).ToArray());
                    if (cleanPhone.Length > 10) cleanPhone = cleanPhone.Substring(cleanPhone.Length - 10);
                    formFields["mobileNum"] = cleanPhone;
                }

                // Calculate merchantHashedReq if SecretKey is provided
                if (!string.IsNullOrWhiteSpace(_options.Easypaisa.SecretKey))
                {
                    var rawToHash = $"{amountFormatted}&{orderRef}&{cleanPostBackUrl}&{storeId}&{_options.Easypaisa.SecretKey.Trim()}";
                    using var sha = SHA256.Create();
                    var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawToHash));
                    formFields["merchantHashedReq"] = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }

                result.IsSuccess = true;
                result.RequiresAutoSubmitForm = true;
                result.FormActionUrl = _options.Easypaisa.CheckoutUrl;
                result.FormMethod = "POST";
                result.FormFields = formFields;
                result.GatewayReference = orderRef;

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception preparing Easypaisa payment for {Ref}", request.InternalReference);
                result.IsSuccess = false;
                result.ErrorMessage = "Failed to generate Easypaisa payment checkout.";
                return Task.FromResult(result);
            }
        }

        public async Task<PaymentVerifyResult> VerifyPaymentAsync(PaymentCallbackData callbackData, CancellationToken cancellationToken = default)
        {
            var result = new PaymentVerifyResult
            {
                GatewayName = GatewayName,
                InternalReference = callbackData.InternalReference
            };

            var fields = callbackData.FormFields;
            if (fields.Count == 0 && callbackData.QueryParameters.Count > 0)
            {
                fields = callbackData.QueryParameters;
            }

            if (fields.ContainsKey("simulated") || (callbackData.GatewayReference != null && callbackData.GatewayReference.StartsWith("EP-SIM-")) || callbackData.QueryParameters.ContainsKey("simulated"))
            {
                decimal verifiedAmount = 0;
                if (fields.TryGetValue("amount", out var aStr) && decimal.TryParse(aStr, out var pAmt))
                    verifiedAmount = pAmt;
                else if (callbackData.QueryParameters.TryGetValue("amount", out var qStr) && decimal.TryParse(qStr, out var qAmt))
                    verifiedAmount = qAmt;

                result.IsVerified = true;
                result.IsPaymentSuccessful = true;
                result.Status = "Completed";
                result.VerifiedAmount = verifiedAmount > 0 ? verifiedAmount : (decimal?)null;
                result.VerifiedCurrency = "PKR";
                result.GatewayReference = callbackData.GatewayReference ?? "EP-SIM-DONE";
                return result;
            }

            // Easypaisa parameters: responseCode, desc, orderRefNumber, transactionId
            fields.TryGetValue("responseCode", out var responseCode);
            fields.TryGetValue("desc", out var desc);
            fields.TryGetValue("orderRefNumber", out var orderRef);
            fields.TryGetValue("transactionId", out var gatewayTxnId);

            if (string.IsNullOrWhiteSpace(orderRef) && !string.IsNullOrWhiteSpace(callbackData.InternalReference))
            {
                orderRef = callbackData.InternalReference;
            }

            result.GatewayReference = gatewayTxnId ?? orderRef;
            result.ErrorCode = responseCode;
            result.ErrorMessage = desc;

            // 1. Check response code (Easypaisa success is "0000")
            bool codeSuccess = responseCode == "0000";

            // 2. Server-to-server inquire-transaction check if configured
            bool inquiryVerified = false;
            if (IsConfigured && !string.IsNullOrWhiteSpace(orderRef))
            {
                inquiryVerified = await ExecuteInquireTransactionAsync(orderRef, cancellationToken);
            }

            if (codeSuccess || inquiryVerified)
            {
                result.IsVerified = true;
                result.IsPaymentSuccessful = true;
                result.Status = "Completed";
                result.VerifiedCurrency = "PKR";
            }
            else
            {
                result.IsVerified = true;
                result.IsPaymentSuccessful = false;
                result.Status = (responseCode == "0001" || responseCode == "0002") ? "Failed" : "Cancelled";
            }

            return result;
        }

        private async Task<bool> ExecuteInquireTransactionAsync(string orderRef, CancellationToken cancellationToken)
        {
            try
            {
                var payload = new
                {
                    orderId = orderRef,
                    storeId = _options.Easypaisa.StoreId.Trim(),
                    accountNum = _options.Easypaisa.Username.Trim()
                };

                var json = JsonSerializer.Serialize(payload);
                using var httpReq = new HttpRequestMessage(HttpMethod.Post, _options.Easypaisa.InquiryUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                httpReq.Headers.Add("User-Agent", "NGEOS-Payment-Engine/1.0");

                using var response = await _httpClient.SendAsync(httpReq, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("responseCode", out var codeElem))
                    {
                        var code = codeElem.GetString();
                        return code == "0000";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Easypaisa inquire-transaction error for {Ref}", orderRef);
            }

            return false;
        }

        public async Task<PaymentRefundResult> ProcessRefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken = default)
        {
            var result = new PaymentRefundResult();

            if (!IsConfigured)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Easypaisa credentials not configured.";
                return result;
            }

            try
            {
                var payload = new
                {
                    orderId = request.InternalReference,
                    transactionId = request.GatewayReference,
                    storeId = _options.Easypaisa.StoreId.Trim(),
                    amount = Math.Round(request.RefundAmount, 2).ToString("F2")
                };

                var json = JsonSerializer.Serialize(payload);
                using var httpReq = new HttpRequestMessage(HttpMethod.Post, _options.Easypaisa.RefundUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var response = await _httpClient.SendAsync(httpReq, cancellationToken);
                var respBody = await response.Content.ReadAsStringAsync(cancellationToken);
                result.RawResponse = respBody;

                if (response.IsSuccessStatusCode)
                {
                    result.IsSuccess = true;
                    result.RefundedAmount = request.RefundAmount;
                    result.RefundReference = $"EP-REF-{Guid.NewGuid():N}".Substring(0, 18).ToUpper();
                    return result;
                }

                result.IsSuccess = false;
                result.ErrorMessage = $"Easypaisa refund returned HTTP {response.StatusCode}.";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception processing Easypaisa refund for {Ref}", request.GatewayReference);
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public async Task<GatewayHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var result = new GatewayHealthResult
            {
                GatewayName = GatewayName,
                IsConfigured = IsConfigured,
                Environment = EnvironmentName
            };

            if (!IsConfigured)
            {
                result.IsHealthy = false;
                result.StatusMessage = "Credentials Missing";
                return result;
            }

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                using var httpReq = new HttpRequestMessage(HttpMethod.Head, _options.Easypaisa.CheckoutUrl);
                using var resp = await _httpClient.SendAsync(httpReq, cancellationToken);
                stopwatch.Stop();

                result.IsHealthy = true;
                result.StatusMessage = EnvironmentName == "Production" ? "Production Ready" : "Sandbox Ready";
                result.PingLatencyMs = $"{stopwatch.ElapsedMilliseconds}ms";
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Easypaisa health check ping failed: {Message}", ex.Message);
                result.IsHealthy = false;
                result.StatusMessage = "Connection Failed";
            }

            return result;
        }
    }
}
