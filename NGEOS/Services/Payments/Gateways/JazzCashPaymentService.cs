using System;
using System.Collections.Generic;
using System.Linq;
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
    /// JazzCash Pakistan payment gateway integration service.
    /// Implements hosted checkout auto-submit form, HMAC-SHA256 secure hash calculation,
    /// callback verification, DoInquiry server verification, and refunds.
    /// Official Sandbox Docs: https://sandbox.jazzcash.com.pk/SandboxDocumentation/index.html
    /// </summary>
    public class JazzCashPaymentService : IPaymentGateway
    {
        public const string HttpClientName = "JazzCashClient";

        private readonly HttpClient _httpClient;
        private readonly PaymentOptions _options;
        private readonly ILogger<JazzCashPaymentService> _logger;

        public string GatewayName => "JazzCash";
        public bool IsConfigured => _options.JazzCash.IsConfigured;
        public string EnvironmentName => _options.JazzCash.Environment;

        public PaymentGatewayCapabilities Capabilities => new()
        {
            SupportsHostedCheckout = true,
            SupportsDirectApi = true,
            SupportsWebhooks = true,
            SupportsAutomatedRefunds = true,
            SupportsPartialRefunds = false,
            SupportedCurrencies = new[] { "PKR" },
            SupportedPaymentMethods = new[] { "JazzCash Mobile Account", "Debit/Credit Card", "Voucher (OTC)" }
        };

        public JazzCashPaymentService(
            IHttpClientFactory httpClientFactory,
            IOptions<PaymentOptions> options,
            ILogger<JazzCashPaymentService> logger)
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
                result.ErrorMessage = "JazzCash merchant credentials are not configured.";
                return Task.FromResult(result);
            }

            if (_options.SimulationMode || _options.JazzCash.IsSimulation)
            {
                result.IsSuccess = true;
                result.RequiresAutoSubmitForm = false;
                result.RedirectUrl = $"/payments/simulator?ref={request.InternalReference}&gateway={GatewayName}";
                result.GatewayReference = $"JC-SIM-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
                return Task.FromResult(result);
            }

            try
            {
                // JazzCash operates strictly in Pakistan Standard Time (PKT = UTC+5)
                var pktNow = DateTime.UtcNow.AddHours(5);
                var txnDateTime = pktNow.ToString("yyyyMMddHHmmss");
                var txnExpiry = pktNow.AddHours(1).ToString("yyyyMMddHHmmss");

                // JazzCash expects amount in paisas (multiply by 100) or exact formatted integer
                var amountInPaisa = ((long)Math.Round(request.Amount * 100m)).ToString();

                var txnRefNo = "T" + pktNow.ToString("yyyyMMddHHmmssfff");
                var billRef = request.InternalReference;

                // JazzCash WAF strictly rejects query strings in return URL (e.g. ?ref=...)
                // JazzCash passes pp_BillReference in the POST callback body.
                // Allow custom JAZZCASH_RETURN_URL in .env (for Ngrok / public domain during local testing)
                var cleanReturnUrl = !string.IsNullOrWhiteSpace(_options.JazzCash.ReturnUrl)
                    ? _options.JazzCash.ReturnUrl.Trim()
                    : request.ReturnUrl;

                if (cleanReturnUrl.Contains("?"))
                {
                    cleanReturnUrl = cleanReturnUrl.Substring(0, cleanReturnUrl.IndexOf('?'));
                }

                var formFields = new Dictionary<string, string>
                {
                    { "pp_Version", "1.1" },
                    { "pp_TxnType", "" }, // Blank allows donor on JazzCash checkout to pick Mobile Account or Card
                    { "pp_Language", "EN" },
                    { "pp_MerchantID", _options.JazzCash.MerchantId.Trim() },
                    { "pp_Password", _options.JazzCash.Password.Trim() },
                    { "pp_TxnRefNo", txnRefNo },
                    { "pp_Amount", amountInPaisa },
                    { "pp_TxnCurrency", "PKR" },
                    { "pp_TxnDateTime", txnDateTime },
                    { "pp_BillReference", billRef },
                    { "pp_Description", "NGEOS Donation" },
                    { "pp_TxnExpiryDateTime", txnExpiry },
                    { "pp_ReturnURL", cleanReturnUrl }
                };

                // Calculate HMAC-SHA256 pp_SecureHash
                var secureHash = CalculateSecureHash(formFields, _options.JazzCash.IntegritySalt.Trim());
                formFields["pp_SecureHash"] = secureHash;

                result.IsSuccess = true;
                result.RequiresAutoSubmitForm = true;
                result.FormActionUrl = _options.JazzCash.CheckoutUrl;
                result.FormMethod = "POST";
                result.FormFields = formFields;
                result.GatewayReference = txnRefNo;

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception preparing JazzCash payment form for {Ref}", request.InternalReference);
                result.IsSuccess = false;
                result.ErrorMessage = "Failed to generate JazzCash payment request.";
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

            if (fields.ContainsKey("simulated") || (callbackData.GatewayReference != null && callbackData.GatewayReference.StartsWith("JC-SIM-")) || callbackData.QueryParameters.ContainsKey("simulated"))
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
                result.GatewayReference = callbackData.GatewayReference ?? "JC-SIM-DONE";
                return result;
            }

            if (!fields.TryGetValue("pp_ResponseCode", out var responseCode))
            {
                result.IsVerified = false;
                result.Status = "Pending";
                result.ErrorMessage = "JazzCash response code missing from callback.";
                return result;
            }

            fields.TryGetValue("pp_ResponseMessage", out var responseMsg);
            fields.TryGetValue("pp_TxnRefNo", out var txnRefNo);
            fields.TryGetValue("pp_Amount", out var amountStr);
            fields.TryGetValue("pp_SecureHash", out var receivedHash);

            result.GatewayReference = txnRefNo;
            result.ErrorCode = responseCode;
            result.ErrorMessage = responseMsg;

            // 1. Verify response signature
            bool isHashValid = false;
            if (!string.IsNullOrWhiteSpace(receivedHash) && !string.IsNullOrWhiteSpace(_options.JazzCash.IntegritySalt))
            {
                var copyDict = new Dictionary<string, string>(fields);
                copyDict.Remove("pp_SecureHash");
                var expectedHash = CalculateSecureHash(copyDict, _options.JazzCash.IntegritySalt.Trim());
                isHashValid = expectedHash.Equals(receivedHash.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            // 2. Parse verified amount
            if (long.TryParse(amountStr, out var paisas))
            {
                result.VerifiedAmount = paisas / 100m;
                result.VerifiedCurrency = "PKR";
            }

            // 3. Server-to-server DoInquiry check if configured
            bool inquiryVerified = false;
            if (IsConfigured && !string.IsNullOrWhiteSpace(txnRefNo))
            {
                inquiryVerified = await ExecuteDoInquiryAsync(txnRefNo, cancellationToken);
            }

            // JazzCash success code is "000" or "0000"
            bool isSuccessful = (responseCode == "000" || responseCode == "0000") && (isHashValid || inquiryVerified);

            if (isSuccessful)
            {
                result.IsVerified = true;
                result.IsPaymentSuccessful = true;
                result.Status = "Completed";
            }
            else
            {
                result.IsVerified = isHashValid || inquiryVerified;
                result.IsPaymentSuccessful = false;
                result.Status = (responseCode == "124" || responseCode == "157") ? "Cancelled" : "Failed";
            }

            return result;
        }

        private async Task<bool> ExecuteDoInquiryAsync(string txnRefNo, CancellationToken cancellationToken)
        {
            try
            {
                var inquiryFields = new Dictionary<string, string>
                {
                    { "pp_MerchantID", _options.JazzCash.MerchantId.Trim() },
                    { "pp_Password", _options.JazzCash.Password.Trim() },
                    { "pp_TxnRefNo", txnRefNo.Trim() }
                };

                inquiryFields["pp_SecureHash"] = CalculateSecureHash(inquiryFields, _options.JazzCash.IntegritySalt.Trim());

                var json = JsonSerializer.Serialize(inquiryFields);
                using var httpReq = new HttpRequestMessage(HttpMethod.Post, _options.JazzCash.InquiryUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var response = await _httpClient.SendAsync(httpReq, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("pp_ResponseCode", out var codeElem))
                    {
                        var code = codeElem.GetString();
                        return code == "000" || code == "0000";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JazzCash DoInquiry network error for {Ref}", txnRefNo);
            }

            return false;
        }

        public async Task<PaymentRefundResult> ProcessRefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken = default)
        {
            var result = new PaymentRefundResult();

            if (!IsConfigured)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "JazzCash credentials not configured.";
                return result;
            }

            try
            {
                var now = DateTime.UtcNow;
                var amountInPaisa = ((long)Math.Round(request.RefundAmount * 100m)).ToString();
                var refundTxnRef = "R" + now.ToString("yyyyMMddHHmmssfff");

                var refundFields = new Dictionary<string, string>
                {
                    { "pp_MerchantID", _options.JazzCash.MerchantId.Trim() },
                    { "pp_Password", _options.JazzCash.Password.Trim() },
                    { "pp_TxnRefNo", request.GatewayReference },
                    { "pp_Amount", amountInPaisa },
                    { "pp_TxnCurrency", "PKR" }
                };

                refundFields["pp_SecureHash"] = CalculateSecureHash(refundFields, _options.JazzCash.IntegritySalt.Trim());

                var json = JsonSerializer.Serialize(refundFields);
                using var httpReq = new HttpRequestMessage(HttpMethod.Post, _options.JazzCash.RefundUrl)
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
                    result.RefundReference = refundTxnRef;
                    return result;
                }

                result.IsSuccess = false;
                result.ErrorMessage = $"JazzCash refund returned HTTP {response.StatusCode}.";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception processing JazzCash refund for {Ref}", request.GatewayReference);
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
                using var httpReq = new HttpRequestMessage(HttpMethod.Head, _options.JazzCash.CheckoutUrl);
                using var resp = await _httpClient.SendAsync(httpReq, cancellationToken);
                stopwatch.Stop();

                result.IsHealthy = true;
                result.StatusMessage = EnvironmentName == "Production" ? "Production Ready" : "Sandbox Ready";
                result.PingLatencyMs = $"{stopwatch.ElapsedMilliseconds}ms";
            }
            catch (Exception ex)
            {
                _logger.LogWarning("JazzCash health check ping failed: {Message}", ex.Message);
                result.IsHealthy = false;
                result.StatusMessage = "Connection Failed";
            }

            return result;
        }

        /// <summary>
        /// Calculates JazzCash HMAC-SHA256 signature by sorting non-empty fields alphabetically,
        /// concatenating their values with '&', and signing with the integrity salt.
        /// </summary>
        public static string CalculateSecureHash(Dictionary<string, string> fields, string salt)
        {
            var sortedFields = fields
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value) && !kvp.Key.Equals("pp_SecureHash", StringComparison.OrdinalIgnoreCase))
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => kvp.Value);

            var rawStringToHash = salt + "&" + string.Join("&", sortedFields);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawStringToHash));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
        }
    }
}
