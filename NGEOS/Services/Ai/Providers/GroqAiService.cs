using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NGEOS.Services.Ai.Dtos;

namespace NGEOS.Services.Ai.Providers
{
    /// <summary>
    /// Primary AI provider service using the Groq Cloud API (Llama models).
    /// </summary>
    public class GroqAiService : IAiProviderService
    {
        public const string HttpClientName = "GroqAiClient";
        private readonly HttpClient _httpClient;
        private readonly AiChatbotOptions _options;
        private readonly ILogger<GroqAiService> _logger;

        public string ProviderName => "Groq";
        public bool IsConfigured => _options.IsGroqConfigured;

        public GroqAiService(
            IHttpClientFactory httpClientFactory,
            IOptions<AiChatbotOptions> options,
            ILogger<GroqAiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient(HttpClientName);
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string?> GenerateReplyAsync(
            string systemPrompt,
            List<ChatMessageItem> history,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("Groq AI is not configured or API key is missing. Skipping Groq.");
                return null;
            }

            try
            {
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // Add up to MaxHistoryMessages recent conversation turns
                if (history != null && history.Count > 0)
                {
                    var countToTake = Math.Min(history.Count, _options.MaxHistoryMessages);
                    var startIndex = history.Count - countToTake;
                    for (int i = startIndex; i < history.Count; i++)
                    {
                        var item = history[i];
                        var role = item.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
                        messages.Add(new { role, content = item.Content });
                    }
                }

                // Add current user message
                messages.Add(new { role = "user", content = userMessage });

                var requestPayload = new
                {
                    model = string.IsNullOrWhiteSpace(_options.GroqModel) ? "llama-3.3-70b-versatile" : _options.GroqModel,
                    messages = messages,
                    temperature = 0.5,
                    max_tokens = 1024
                };

                var jsonContent = JsonSerializer.Serialize(requestPayload);
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.GroqApiKey.Trim());
                request.Headers.Add("User-Agent", "NGEOS-Assistant/1.0 (Windows NT 10.0; Win64; x64)");

                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Groq API returned HTTP {StatusCode}. Body preview: {Preview}", 
                        response.StatusCode, 
                        errorBody.Length > 200 ? errorBody.Substring(0, 200) : errorBody);
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseBody);

                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content))
                    {
                        var text = content.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text.Trim();
                        }
                    }
                }

                _logger.LogWarning("Groq API returned a 200 OK response but no usable choice content.");
                return null;
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Groq API call timed out after configured duration.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception occurred while communicating with Groq API.");
                return null;
            }
        }
    }
}
