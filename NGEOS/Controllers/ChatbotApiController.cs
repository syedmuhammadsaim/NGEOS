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
        private readonly AiChatbotOptions _options;
        private readonly ILogger<ChatbotApiController> _logger;

        public ChatbotApiController(
            IAiChatbotService chatbotService,
            IOptions<AiChatbotOptions> options,
            ILogger<ChatbotApiController> logger)
        {
            _chatbotService = chatbotService;
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

            // Determine user role context
            string userRole = "Guest / Donor";
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    userRole = "NGEOS Platform Administrator";
                }
                else
                {
                    var name = User.Identity.Name ?? "Registered Member";
                    userRole = $"Registered Member ({name})";
                }
            }

            try
            {
                var result = await _chatbotService.ProcessMessageAsync(request, userRole, cancellationToken);
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
