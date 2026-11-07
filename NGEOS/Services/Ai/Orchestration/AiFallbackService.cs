using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NGEOS.Services.Ai.Dtos;
using NGEOS.Services.Ai.Grounding;
using NGEOS.Services.Ai.Providers;

namespace NGEOS.Services.Ai.Orchestration
{
    /// <summary>
    /// Fallback orchestrator that enriches chat prompts with live EF Core database grounding
    /// and seamlessly handles Groq -> Gemini failover.
    /// </summary>
    public class AiFallbackService : IAiChatbotService
    {
        private readonly GroqAiService _groqService;
        private readonly GeminiAiService _geminiService;
        private readonly INgeosKnowledgeService _knowledgeService;
        private readonly AiChatbotOptions _options;
        private readonly ILogger<AiFallbackService> _logger;

        public const string StandardUnavailableMessage = "Sorry, the AI assistant is temporarily unavailable. Please try again later.";

        public AiFallbackService(
            GroqAiService groqService,
            GeminiAiService geminiService,
            INgeosKnowledgeService knowledgeService,
            IOptions<AiChatbotOptions> options,
            ILogger<AiFallbackService> logger)
        {
            _groqService = groqService;
            _geminiService = geminiService;
            _knowledgeService = knowledgeService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ChatbotResponseDto> ProcessMessageAsync(
            ChatbotRequestDto request, 
            string? userRole = null, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new ChatbotResponseDto();

            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                response.IsSuccess = false;
                response.Reply = "Please enter a message to begin the conversation.";
                response.SuggestedActions = GetDefaultSuggestionChips();
                return response;
            }

            // 1. Fetch verified live grounding context from SQL Server
            string liveContext = await _knowledgeService.GetGroundingContextAsync(request.Message, cancellationToken);

            // 2. Build multi-language system prompt
            string systemPrompt = BuildSystemPrompt(liveContext, userRole);

            string? aiReply = null;
            string providerUsed = "None";

            bool preferGroq = _options.PrimaryProvider.Equals("Groq", StringComparison.OrdinalIgnoreCase);
            IAiProviderService primary = preferGroq ? _groqService : _geminiService;
            IAiProviderService fallback = preferGroq ? _geminiService : _groqService;

            // 3. Attempt Primary Provider
            if (primary.IsConfigured)
            {
                try
                {
                    aiReply = await primary.GenerateReplyAsync(
                        systemPrompt, 
                        request.ConversationHistory, 
                        request.Message, 
                        cancellationToken);

                    if (!string.IsNullOrWhiteSpace(aiReply))
                    {
                        providerUsed = primary.ProviderName;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Primary AI provider {Provider} threw an exception. Initiating fallback.", primary.ProviderName);
                }
            }
            else
            {
                _logger.LogInformation("Primary provider {Provider} is not configured or lacks API key. Directing to fallback.", primary.ProviderName);
            }

            // 4. Automatic Fallback to Secondary Provider if Primary failed
            if (string.IsNullOrWhiteSpace(aiReply))
            {
                if (fallback.IsConfigured)
                {
                    _logger.LogInformation("Attempting automatic fallback to {Provider}...", fallback.ProviderName);
                    try
                    {
                        aiReply = await fallback.GenerateReplyAsync(
                            systemPrompt, 
                            request.ConversationHistory, 
                            request.Message, 
                            cancellationToken);

                        if (!string.IsNullOrWhiteSpace(aiReply))
                        {
                            providerUsed = fallback.ProviderName;
                            _logger.LogInformation("Automatic fallback to {Provider} succeeded.", fallback.ProviderName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fallback AI provider {Provider} also failed.", fallback.ProviderName);
                    }
                }
                else
                {
                    _logger.LogWarning("Fallback provider {Provider} is also not configured or lacks API key.", fallback.ProviderName);
                }
            }

            stopwatch.Stop();

            // 5. Final Response Formatting
            if (!string.IsNullOrWhiteSpace(aiReply))
            {
                response.IsSuccess = true;
                response.Reply = aiReply;
                response.ProviderUsed = providerUsed;
                response.SuggestedActions = GenerateContextualChips(request.Message);
                _logger.LogInformation("Chatbot answered successfully via {Provider} in {ElapsedMs}ms", providerUsed, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                response.IsSuccess = false;
                response.Reply = StandardUnavailableMessage;
                response.ProviderUsed = "None";
                response.ErrorMessage = "Both AI providers are temporarily unavailable or not yet configured with active API keys.";
                response.SuggestedActions = GetDefaultSuggestionChips();
                _logger.LogWarning("Chatbot failed to get a reply from both providers in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }

            return response;
        }

        private string BuildSystemPrompt(string liveContext, string? userRole)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are the official AI Assistant for NGEOS (Next Generation NGO Management & Donation Platform, also branded as Give-AID).");
            sb.AppendLine("Your mission is to provide accurate, helpful, and transparent guidance to donors, volunteers, NGO representatives, and visitors.");

            if (!string.IsNullOrWhiteSpace(userRole))
            {
                sb.AppendLine($"Current user role: {userRole}. Contextualize your answers accordingly.");
            }

            sb.AppendLine("\n### CRITICAL OPERATING INSTRUCTIONS:");
            sb.AppendLine("1. GROUND TRUTH FIDELITY: You must answer all questions about causes, donation amounts, counts, programmes, and organizations based STRICTLY on the provided [NGEOS VERIFIED LIVE DATABASE CONTEXT].");
            sb.AppendLine("2. FINANCIAL ACCURACY: ONLY completed transactions count towards total raised funds. Pending, failed, or cancelled transactions are excluded. Do not guess, estimate, or fabricate any financial numbers or progress percentages.");
            sb.AppendLine("3. ANTI-HALLUCINATION: If a specific campaign, programme, or policy is not in the verified context, explicitly state that it is not currently recorded or published on NGEOS.");
            sb.AppendLine("4. PRIVACY PROTECTION: Never share or solicit private personal information such as donor email addresses, phone numbers, or credit card details.");
            sb.AppendLine("5. MULTI-LANGUAGE FLUENCY: Respond naturally in the exact language used by the user. If the user writes in English, reply in English. If the user writes in Roman Urdu (e.g. 'Donation kaise karein?', 'Total kitna paisa aya hai?'), reply in polite, fluent Roman Urdu. If the user writes in Urdu script, reply in Urdu script.");
            sb.AppendLine("6. FORMATTING: Use clean markdown with clear headings, bullet points, and bold text for readability.");
            sb.AppendLine("7. ACTIONABLE GUIDANCE: Where relevant, tell users where to go on the website (e.g., browse Causes at /Causes, see Programmes at /Programmes, donate via the 'Donate Now' button, or explore career/volunteer roles at /About/Career).");

            sb.AppendLine("\n" + liveContext);

            return sb.ToString();
        }

        private List<string> GenerateContextualChips(string userMessage)
        {
            var msg = userMessage.ToLowerInvariant();

            if (msg.Contains("cause") || msg.Contains("campaign") || msg.Contains("orphan") || msg.Contains("school"))
            {
                return new List<string> { "How to Donate?", "Total Verified Funds", "Active Programmes", "Zakat Calculator" };
            }

            if (msg.Contains("donate") || msg.Contains("zakat") || msg.Contains("fund") || msg.Contains("money"))
            {
                return new List<string> { "Active Causes", "Payment Methods", "Tax & Receipts", "Volunteer Roles" };
            }

            if (msg.Contains("volunteer") || msg.Contains("job") || msg.Contains("career") || msg.Contains("work"))
            {
                return new List<string> { "Active Causes", "Community Programmes", "Partner NGOs", "Contact Support" };
            }

            return GetDefaultSuggestionChips();
        }

        private List<string> GetDefaultSuggestionChips()
        {
            return new List<string>
            {
                "Active Causes",
                "How to Donate?",
                "Total Funds Raised",
                "Volunteer Opportunities",
                "Zakat & Sadqah"
            };
        }
    }
}
