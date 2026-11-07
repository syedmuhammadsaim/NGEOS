using System;
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
    /// Safepay Pakistan payment integration service.
    /// Implements payment initialization, hosted checkout redirect,
    /// server verification, HMAC webhook authentication, and refunds.
    /// Official Docs: https://apidocs.getsafepay.com/
    /// </summary>
    public class SafepayPaymentService : IPaymentGateway
    {
        public const string HttpClientName = "SafepayClient";

        private readonly HttpClient _httpClient;
        private readonly PaymentOptions _options;
        private readonly ILogger<SafepayPaymentService> _logger;

        public string GatewayName => "Safepay";
        public bool IsConfigured => _options.Safepay.IsConfigured;
        public string EnvironmentName => _options.Safepay.Environment;

        public PaymentGatewayCapabilities Capabilities => new()
        {
            SupportsHostedCheckout = true,
            SupportsDirectApi = false,
            SupportsWebhooks = true,
            SupportsAutomatedRefunds = true,
            SupportsPartialRefunds = true,
            SupportedCurrencies = new[] { "PKR", "USD" },
            SupportedPaymentMethods = new[] { "Visa", "MasterCard", "PayPak", "UnionPay", "Bank Accounts" }
        };

        public SafepayPaymentService(
            IHttpClientFactory httpClientFactory,
            IOptions<PaymentOptions> options,
            ILogger<SafepayPaymentService> logger)
        {
            _httpClient = httpClientFactory.CreateClient(HttpClientName);
            _options = options.Value;
            _logger = logger;
        }

        public async Task<PaymentInitiateResult> InitiatePaymentAsync(PaymentInitiateRequest request, CancellationToken cancellationToken = default)
        {
            var result = new PaymentInitiateResult { GatewayName = GatewayName };

            if (!IsConfigured)
            {
                result.IsSuccess = false;
                result.ErrorCode = "CONFIG_MISSING";
                result.ErrorMessage = "Safepay API credentials are not configured.";
                return result;
            }

            if (_options.SimulationMode || _options.Safepay.IsSimulation)
            {
                result.IsSuccess = true;
                                result.RequiresAutoSubmitForm = false;
                result.RedirectUrl = $"/payments/simulator?ref={request.InternalReference}&gateway={GatewayName}";
                result.GatewayReference = $"SPY-SIM-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
                return result;
            }

            try
            {
                var envStr = _options.Safepay.Environment.ToLowerInvariant();
                var endpoint = $"{_options.Safepay.BaseUrl}/order/v1/init";

                var payload = new
                {
                    client = _options.Safepay.ApiKey.Trim(),
                    amount = Math.Round(request.Amount, 2),
                    currency = string.IsNullOrWhiteSpace(request.Currency) ? "PKR" : request.Currency.ToUpperInvariant(),
                    environment = envStr
                };

                var json = JsonSerializer.Serialize(payload);
                using var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                httpReq.Headers.Add("X-SFPY-MERCHANT-SECRET", _options.Safepay.SecretKey.Trim());
                httpReq.Headers.Add("User-Agent", "NGEOS-Payment-Engine/1.0");

                using var response = await _httpClient.SendAsync(httpReq, cancellationToken);
                var respBody = await response.Content.ReadAsStringAsync(cancellationToken);
                result.RawPayload = respBody;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Safepay init failed with HTTP {Status}: {Body}", response.StatusCode, respBody);
                    result.IsSuccess = false;
                    result.ErrorCode = $"HTTP_{response.StatusCode}";
                    result.ErrorMessage = "Failed to create Safepay payment session.";
                    return result;
                }

                using var doc = JsonDocument.Parse(respBody);
                if (doc.RootElement.TryGetProperty("data", out var dataElem) &&
                    dataElem.TryGetProperty("token", out var tokenElem))
                {
                    var trackerToken = tokenElem.GetString();
                    result.IsSuccess = true;
                    result.GatewayReference = trackerToken;
                    result.RedirectUrl = $"{_options.Safepay.CheckoutBaseUrl}?beacon={trackerToken}&env={envStr}";
                    return result;
                }

                result.IsSuccess = false;
                result.ErrorMessage = "Safepay did not return a valid tracking token.";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while initiating Safepay payment for {Ref}", request.InternalReference);
                result.IsSuccess = false;
                result.ErrorMessage = "Safepay connection error.";
                return result;
            }
        }

        public async Task<PaymentVerifyResult> VerifyPaymentAsync(PaymentCallbackData callbackData, CancellationToken cancellationToken = default)
        {
            var result = new PaymentVerifyResult
            {
                GatewayName = GatewayName,
                InternalReference = callbackData.InternalReference
            };

            // Safepay callback passes tracker token or tracker in query or form
            string tracker = callbackData.GatewayReference ?? "";
            if (string.IsNullOrWhiteSpace(tracker) && callbackData.QueryParameters.TryGetValue("tracker", out var qTracker))
            {
                tracker = qTracker;
            }
            if (string.IsNullOrWhiteSpace(tracker) && callbackData.QueryParameters.TryGetValue("beacon", out var qBeacon))
            {
                tracker = qBeacon;
            }

            result.GatewayReference = tracker;

            if (callbackData.QueryParameters.ContainsKey("simulated") || callbackData.FormFields.ContainsKey("simulated") || (tracker != null && tracker.StartsWith("SPY-SIM-")))
            {
                decimal verifiedAmount = 0;
                if (callbackData.QueryParameters.TryGetValue("amount", out var aStr) && decimal.TryParse(aStr, out var pAmt))
                    verifiedAmount = pAmt;
                else if (callbackData.FormFields.TryGetValue("amount", out var fStr) && decimal.TryParse(fStr, out var fAmt))
                    verifiedAmount = fAmt;

                result.IsVerified = true;
                result.IsPaymentSuccessful = true;
                result.Status = "Completed";
                result.VerifiedAmount = verifiedAmount > 0 ? verifiedAmount : (decimal?)null;
                result.VerifiedCurrency = "PKR";
                result.GatewayReference = tracker ?? "SPY-SIM-DONE";
                return result;
            }

            if (string.IsNullOrWhiteSpace(tracker))
            {
                result.IsVerified = false;
                result.Status = "Failed";
                result.ErrorMessage = "Safepay tracker token missing in callback.";
                return result;
            }

            try
            {
                // Server-to-server verification GET /order/v1/{tracker}
                var endpoint = $"{_options.Safepay.BaseUrl}/order/v1/{tracker}";
                using var httpReq = new HttpRequestMessage(HttpMethod.Get, endpoint);
                httpReq.Headers.Add("X-SFPY-MERCHANT-SECRET", _options.Safepay.SecretKey.Trim());
                httpReq.Headers.Add("User-Agent", "NGEOS-Payment-Engine/1.0");

                using var response = await _httpClient.SendAsync(httpReq, cancellationToken);
                var respBody = await response.Content.ReadAsStringAsync(cancellationToken);
                result.RawResponse = respBody;

                if (!response.IsSuccessStatusCode)
                {
                    result.IsVerified = false;
                    result.Status = "Failed";
                    result.ErrorMessage = $"Safepay verification inquiry returned HTTP {response.StatusCode}.";
                    return result;
                }

                using var doc = JsonDocument.Parse(respBody);
                if (doc.RootElement.TryGetProperty("data", out var dataElem))
                {
                    string state = dataElem.TryGetProperty("state", out var stateElem) ? stateElem.GetString() ?? "" : "";
                    decimal amount = 0m;
                    if (dataElem.TryGetProperty("amount", out var amountElem))
                    {
                        if (amountElem.ValueKind == JsonValueKind.Number) amount = amountElem.GetDecimal();
                    }
                    string currency = dataElem.TryGetProperty("currency", out var currElem) ? currElem.GetString() ?? "PKR" : "PKR";

                    result.VerifiedAmount = amount;
                    result.VerifiedCurrency = currency;

                    if (state.Equals("PAID", StringComparison.OrdinalIgnoreCase) ||
                        state.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsVerified = true;
                        result.IsPaymentSuccessful = true;
                        result.Status = "Completed";
                    }
                    else if (state.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsVerified = true;
                        result.IsPaymentSuccessful = false;
                        result.Status = "Pending";
                    }
                    else
                    {
                        result.IsVerified = true;
                        result.IsPaymentSuccessful = false;
                        result.Status = "Failed";
                        result.ErrorMessage = $"Safepay order status is {state}.";
                    }
                    return result;
                }

                result.IsVerified = false;
                result.Status = "Failed";
                result.ErrorMessage = "Unable to parse Safepay order data.";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception verifying Safepay transaction {Tracker}", tracker);
                result.IsVerified = false;
                result.Status = "Pending";
                result.ErrorMessage = "Safepay verification error. Will reconcile via background webhook.";
                return result;
            }
        }

        public async Task<PaymentRefundResult> ProcessRefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken = default)
        {
            var result = new PaymentRefundResult();

            if (!IsConfigured)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Safepay credentials not configured.";
                return result;
            }

            try
            {
                var endpoint = $"{_options.Safepay.BaseUrl}/order/v1/refund";
                var payload = new
                {
                    tracker = request.GatewayReference,
                    amount = Math.Round(request.RefundAmount, 2),
                    reason = request.Reason
                };

                var json = JsonSerializer.Serialize(payload);
                using var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                httpReq.Headers.Add("X-SFPY-MERCHANT-SECRET", _options.Safepay.SecretKey.Trim());
                httpReq.Headers.Add("User-Agent", "NGEOS-Payment-Engine/1.0");

                using var response = await _httpClient.SendAsync(httpReq, cancellationToken);
                var respBody = await response.Content.ReadAsStringAsync(cancellationToken);
                result.RawResponse = respBody;

                if (response.IsSuccessStatusCode)
                {
                    result.IsSuccess = true;
                    result.RefundedAmount = request.RefundAmount;
                    result.RefundReference = $"SF-REF-{Guid.NewGuid():N}".Substring(0, 18).ToUpper();
                    return result;
                }

                result.IsSuccess = false;
                result.ErrorMessage = $"Safepay refund failed with HTTP {response.StatusCode}.";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception processing Safepay refund for {Ref}", request.GatewayReference);
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
                // Harmless test request
                var endpoint = $"{_options.Safepay.BaseUrl}/order/v1/init";
                using var httpReq = new HttpRequestMessage(HttpMethod.Options, endpoint);
                using var resp = await _httpClient.SendAsync(httpReq, cancellationToken);
                stopwatch.Stop();

                result.IsHealthy = true;
                result.StatusMessage = EnvironmentName == "Production" ? "Production Ready" : "Sandbox Ready";
                result.PingLatencyMs = $"{stopwatch.ElapsedMilliseconds}ms";
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Safepay health check ping failed: {Message}", ex.Message);
                result.IsHealthy = false;
                result.StatusMessage = "Connection Failed";
            }

            return result;
        }

        public bool VerifyWebhookSignature(string rawBody, string signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(_options.Safepay.WebhookSecret))
            {
                return false;
            }

            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.Safepay.WebhookSecret.Trim()));
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
                var computedHex = BitConverter.ToString(computedHash).Replace("-", "").ToLowerInvariant();

                return computedHex.Equals(signatureHeader.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
