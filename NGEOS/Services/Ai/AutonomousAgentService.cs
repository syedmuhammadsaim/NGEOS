using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NGEOS.Services.Ai.Dtos;
using NGEOS.Services.Ai.Orchestration;

namespace NGEOS.Services.Ai
{
    public interface IAutonomousAgentService
    {
        Task<ChatbotResponseDto> QueryUserAgentAsync(
            int? userId, 
            string userName, 
            string message, 
            object? userContext, 
            List<ChatMessageItem>? history, 
            CancellationToken cancellationToken = default);

        Task<ChatbotResponseDto> QueryAdminAgentAsync(
            string query, 
            object? context, 
            CancellationToken cancellationToken = default);

        Task<bool> EvaluateSecurityAsync(
            int? userId, 
            string? userEmail, 
            string? ipAddress, 
            string activityType, 
            string payload, 
            CancellationToken cancellationToken = default);
    }

    public class AutonomousAgentService : IAutonomousAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly IAiChatbotService _fallbackService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AutonomousAgentService> _logger;

        private readonly string _pythonServiceUrl;
        private readonly string _internalKey;

        public AutonomousAgentService(
            HttpClient httpClient,
            IAiChatbotService fallbackService,
            IConfiguration configuration,
            ILogger<AutonomousAgentService> logger)
        {
            _httpClient = httpClient;
            _fallbackService = fallbackService;
            _configuration = configuration;
            _logger = logger;

            _pythonServiceUrl = (_configuration["AI_SERVICE_URL"] ?? "http://127.0.0.1:8001").TrimEnd('/');
            _internalKey = _configuration["AI_SERVICE_SECRET_KEY"] ?? "NGEOS_AI_INTERNAL_SHARED_KEY_2026_SECURE_TOKEN_32X";
        }

        public async Task<ChatbotResponseDto> QueryUserAgentAsync(
            int? userId,
            string userName,
            string message,
            object? userContext,
            List<ChatMessageItem>? history,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_pythonServiceUrl}/api/v1/user/chat";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("X-NGEOS-Internal-Key", _internalKey);

                var payload = new
                {
                    userId = userId,
                    userName = userName,
                    message = message,
                    userContext = userContext,
                    history = history
                };
                request.Content = JsonContent.Create(payload);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                    var reply = data.GetProperty("reply").GetString() ?? "";
                    var provider = data.TryGetProperty("providerUsed", out var p) ? p.GetString() : "PythonAgent";

                    return new ChatbotResponseDto
                    {
                        IsSuccess = true,
                        Reply = reply,
                        ProviderUsed = provider ?? "PythonAgent"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Python AI User Agent service unreachable. Engaging in-process .NET fallback.");
            }

            // In-process fallback via existing AiFallbackService
            return await _fallbackService.ProcessMessageAsync(
                new ChatbotRequestDto { Message = message, ConversationHistory = history ?? new() },
                userName,
                cancellationToken
            );
        }

        public async Task<ChatbotResponseDto> QueryAdminAgentAsync(
            string query,
            object? context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_pythonServiceUrl}/api/v1/admin/query";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("X-NGEOS-Internal-Key", _internalKey);

                var payload = new
                {
                    query = query,
                    context = context
                };
                request.Content = JsonContent.Create(payload);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                    var reply = data.GetProperty("reply").GetString() ?? "";
                    var provider = data.TryGetProperty("providerUsed", out var p) ? p.GetString() : "PythonAdminAgent";

                    return new ChatbotResponseDto
                    {
                        IsSuccess = true,
                        Reply = reply,
                        ProviderUsed = provider ?? "PythonAdminAgent",
                        SuggestedActions = _fallbackService.GetAdminDefaultSuggestionChips()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Python AI Admin Agent service unreachable. Engaging in-process .NET fallback.");
            }

            // Fallback response if Python service is offline
            var fallbackResult = await _fallbackService.ProcessMessageAsync(
                new ChatbotRequestDto { Message = query, IsAdmin = true },
                "Admin",
                cancellationToken
            );
            fallbackResult.SuggestedActions = _fallbackService.GetAdminDefaultSuggestionChips();
            return fallbackResult;
        }

        public async Task<bool> EvaluateSecurityAsync(
            int? userId,
            string? userEmail,
            string? ipAddress,
            string activityType,
            string payload,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_pythonServiceUrl}/api/v1/security/evaluate";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("X-NGEOS-Internal-Key", _internalKey);

                var data = new
                {
                    userId = userId,
                    userEmail = userEmail,
                    ipAddress = ipAddress,
                    activityType = activityType,
                    payload = payload
                };
                request.Content = JsonContent.Create(data);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send activity to Python security engine.");
                return false;
            }
        }
    }
}
