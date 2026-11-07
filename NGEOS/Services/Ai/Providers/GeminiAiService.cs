using System;
using System.Collections.Generic;
using System.Net.Http;
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
    /// Automatic Fallback AI provider service using Google Gemini API (REST endpoint).
    /// </summary>
    public class GeminiAiService : IAiProviderService
    {
        public const string HttpClientName = "GeminiAiClient";
        private readonly HttpClient _httpClient;
        private readonly AiChatbotOptions _options;
        private readonly ILogger<GeminiAiService> _logger;

        public string ProviderName => "Gemini";
        public bool IsConfigured => _options.IsGeminiConfigured;

        public GeminiAiService(
            IHttpClientFactory httpClientFactory,
            IOptions<AiChatbotOptions> options,
            ILogger<GeminiAiService> logger)
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
                _logger.LogWarning("Gemini AI is not configured or API key is missing. Skipping Gemini.");
                return null;
            }

            try
            {
                var model = string.IsNullOrWhiteSpace(_options.GeminiModel) ? "gemini-3-flash-preview" : _options.GeminiModel;
                if (!model.StartsWith("models/"))
                {
                    model = "models/" + model;
                }
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/{model}:generateContent?key={Uri.EscapeDataString(_options.GeminiApiKey.Trim())}";

                // Build contents
                var contents = new List<object>();

                if (history != null && history.Count > 0)
                {
                    var countToTake = Math.Min(history.Count, _options.MaxHistoryMessages);
                    var startIndex = history.Count - countToTake;
                    for (int i = startIndex; i < history.Count; i++)
                    {
                        var item = history[i];
                        // Gemini roles: 'user' and 'model'
                        var role = item.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "model" : "user";
                        contents.Add(new
                        {
                            role = role,
                            parts = new[] { new { text = item.Content } }
                        });
                    }
                }

                // Add current message
                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                });

                var requestPayload = new
                {
                    systemInstruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = contents,
                    generationConfig = new
                    {
                        temperature = 0.5,
                        maxOutputTokens = 1024
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestPayload);
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Gemini API returned HTTP {StatusCode}. Body preview: {Preview}", 
                        response.StatusCode, 
                        errorBody.Length > 200 ? errorBody.Substring(0, 200) : errorBody);
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseBody);

                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var firstPart = parts[0];
                        if (firstPart.TryGetProperty("text", out var textElem))
                        {
                            var text = textElem.GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                return text.Trim();
                            }
                        }
                    }
                }

                _logger.LogWarning("Gemini API returned a 200 OK response but no usable candidate parts text.");
                return null;
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Gemini API call timed out after configured duration.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception occurred while communicating with Gemini API.");
                return null;
            }
        }
    }
}
