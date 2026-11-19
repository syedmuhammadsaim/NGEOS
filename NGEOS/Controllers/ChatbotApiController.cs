using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NGEOS.Services.Ai;
using NGEOS.Services.Ai.Dtos;
using NGEOS.Services.Ai.Orchestration;

namespace NGEOS.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    [Produces("application/json")]
    public class ChatbotApiController : ControllerBase
    {
        private readonly IAiChatbotService _chatbotService;
        private readonly IAutonomousAgentService _autonomousAgentService;
        private readonly AiChatbotOptions _options;
        private readonly ILogger<ChatbotApiController> _logger;

        public ChatbotApiController(
            IAiChatbotService chatbotService,
            IAutonomousAgentService autonomousAgentService,
            IOptions<AiChatbotOptions> options,
            ILogger<ChatbotApiController> logger)
        {
            _chatbotService = chatbotService;
            _autonomousAgentService = autonomousAgentService;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Sends a message to the NGEOS AI assistant and returns a grounded response.
        /// </summary>
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatbotRequestDto request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new ChatbotResponseDto
                {
                    IsSuccess = false,
                    Reply = "Message cannot be empty.",
                    ErrorMessage = "Invalid input payload."
                });
            }

            // Input length restriction (defence-in-depth)
            if (request.Message.Length > 1000)
            {
                return BadRequest(new ChatbotResponseDto
                {
                    IsSuccess = false,
                    Reply = "Message exceeds the maximum limit of 1000 characters. Please shorten your question.",
                    ErrorMessage = "Message too long."
                });
            }

            int? userId = null;
            string userName = "Guest / Donor";
            string userRole = "Guest / Donor";
            string? userEmail = null;

            bool isAdmin = (request.IsAdmin == true) 
                        || (User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
                        || (Request.Headers["Referer"].ToString().Contains("/Admin", StringComparison.OrdinalIgnoreCase));

            if (isAdmin)
            {
                userRole = "Admin";
                userName = "NGEOS Platform Administrator";
                if (User?.Identity?.IsAuthenticated == true)
                {
                    var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(idClaim, out int parsedId)) userId = parsedId;
                    userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                }
            }
            else if (User?.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int parsedId)) userId = parsedId;
                userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                userName = User.Identity.Name ?? "Registered Member";
                userRole = "User";
            }

            // Security evaluation pass (fire and forget / non-blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _autonomousAgentService.EvaluateSecurityAsync(
                        userId, 
                        userEmail, 
                        ip, 
                        "ChatbotMessage", 
                        request.Message, 
                        CancellationToken.None
                    );
                }
                catch { /* ignore background security check errors */ }
            });

            try
            {
                ChatbotResponseDto result;
                if (userRole == "Admin")
                {
                    result = await _autonomousAgentService.QueryAdminAgentAsync(
                        request.Message, 
                        new { role = "Admin", timestamp = DateTime.UtcNow }, 
                        cancellationToken
                    );
                }
                else
                {
                    result = await _autonomousAgentService.QueryUserAgentAsync(
                        userId,
                        userName,
                        request.Message,
                        new { role = userRole },
                        request.ConversationHistory,
                        cancellationToken
                    );
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in ChatbotApiController while processing message.");
                return Ok(new ChatbotResponseDto
                {
                    IsSuccess = false,
                    Reply = AiFallbackService.StandardUnavailableMessage,
                    ProviderUsed = "None",
                    ErrorMessage = "Internal processing exception occurred."
                });
            }
        }

        /// <summary>
        /// Returns public status of the AI chatbot configuration without leaking any secrets.
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                status = "operational",
                primaryProvider = _options.PrimaryProvider,
                fallbackProvider = _options.PrimaryProvider.Equals("Groq", StringComparison.OrdinalIgnoreCase) ? "Gemini" : "Groq",
                isGroqConfigured = _options.IsGroqConfigured,
                isGeminiConfigured = _options.IsGeminiConfigured,
                groqModel = _options.GroqModel,
                geminiModel = _options.GeminiModel
            });
        }
    }
}
