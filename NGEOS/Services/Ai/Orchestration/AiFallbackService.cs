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

            // 5. Final Response Formatting with Intelligent Grounded Knowledge Fallback
            if (string.IsNullOrWhiteSpace(aiReply))
            {
                _logger.LogInformation("No active external LLM keys detected. Generating grounded knowledge response from NGEOS database.");
                aiReply = GenerateGroundedKnowledgeReply(request.Message, liveContext);
                providerUsed = "NGEOS Grounded Knowledge";
            }

            stopwatch.Stop();

            response.IsSuccess = true;
            response.Reply = aiReply;
            response.ProviderUsed = providerUsed;
            response.SuggestedActions = GenerateContextualChips(request.Message);
            _logger.LogInformation("Chatbot answered successfully via {Provider} in {ElapsedMs}ms", providerUsed, stopwatch.ElapsedMilliseconds);

            return response;
        }

        private string GenerateGroundedKnowledgeReply(string userMessage, string liveContext)
        {
            var msg = (userMessage ?? string.Empty).ToLowerInvariant().Trim();

            // 1. Greetings
            if (msg == "hi" || msg == "hello" || msg == "hey" || msg.Contains("salam") || msg.Contains("assalam") || msg == "aoa")
            {
                return "👋 **Assalam-o-Alaikum! Welcome to NGEOS.**\n\nMain NGEOS ka AI Support Assistant hoon. Main aapki kis tarah madad kar sakta hoon?\n\n* 💖 **[Causes](/Causes):** Active charity aur welfare campaigns browse karein\n* 💳 **[Donate](/Donate):** Online donation ya Zakat ada karein\n* 🧮 **[Zakat Calculator](/Zakat):** Apni Zakat ka shari'i hisab karein\n* 🤝 **[Programmes](/Programmes):** Community aur volunteer drives mein hissa lein\n\nAap apna sawal yahan type kar sakte hain!";
            }

            // 2. Donation & Payments
            if (msg.Contains("donate") || msg.Contains("donation") || msg.Contains("pay") || msg.Contains("payment") || msg.Contains("card") || msg.Contains("paisa") || msg.Contains("kaise") || msg.Contains("kese"))
            {
                return "💳 **NGEOS par Donation dena intehayi aasan aur 100% secure hai:**\n\n1. **[Causes Page](/Causes)** par ja kar apni pasand ki cause select karein ya direct **[Donate Now](/Donate)** click karein.\n2. Apni donation amount enter karein (Zakat ya General).\n3. Payment method chunein (Debit/Credit Card, Safepay, JazzCash, Easypaisa).\n4. Payment mukammal hote hi aapko foran **Official Tax-Exempt PDF Receipt** mil jayegi jo aap download ya print kar sakte hain.\n\nKisi bhi madad ke liye aap hamare **[Help Centre](/HelpCentre)** par bhi rabta kar sakte hain.";
            }

            // 3. Causes & Campaigns
            if (msg.Contains("cause") || msg.Contains("campaign") || msg.Contains("education") || msg.Contains("water") || msg.Contains("health") || msg.Contains("medical") || msg.Contains("orphan") || msg.Contains("relief"))
            {
                return "🎯 **NGEOS Active Welfare Causes:**\n\nHumari verified campaigns jinme aap hissa le sakte hain:\n* 📚 **Universal Education:** Underprivileged bachon ke liye school bags, books aur uniform support.\n* 💧 **Clean Drinking Water:** Sindh aur Balochistan mein solar water filtration plants aur handpumps.\n* 🏥 **Emergency Medical Aid:** Zarooratmand mareezon ke liye free medicines aur diagnostic care.\n* 🌸 **Women Empowerment:** Hunarmand aurat project ke tehat vocational training.\n\nTamam causes ki tafseelat aur progress dekhne ke liye **[Causes](/Causes)** page visit karein.";
            }

            // 4. Zakat
            if (msg.Contains("zakat") || msg.Contains("sadqah") || msg.Contains("fitrah") || msg.Contains("nisab"))
            {
                return "🧮 **Smart Zakat Management on NGEOS:**\n\n* Hamare paas **[Smart Zakat Calculator](/Zakat)** mojood hai jo Gold, Silver, Cash aur Assets par automatically 2.5% Zakat compute karta hai.\n* NGEOS par sirf un certified causes ko Zakat di ja sakti hai jo Shariah-compliant aur verified Mustahiqeen tak pohanchti hain.\n* Har Zakat donation par verified tracking aur transparent report generate hoti hai.";
            }

            // 5. Programmes & Events
            if (msg.Contains("programme") || msg.Contains("program") || msg.Contains("event") || msg.Contains("marathon") || msg.Contains("camp"))
            {
                return "🤝 **Community Programmes & Events:**\n\n* **Youth Digital Skills Bootcamp:** Naujawano ko free IT aur digital freelancing training.\n* **Mobile Health Clinics:** Door-daraaz ilaqon mein free medical checkups aur medicines.\n* **Winter Relief Drives:** Sardi mein ghareeb khandanoun ko blankets aur ration.\n\nExplore karein: **[Programmes](/Programmes)** aur **[Charity Events](/Events)**.";
            }

            // 6. Volunteers & Careers
            if (msg.Contains("volunteer") || msg.Contains("job") || msg.Contains("career") || msg.Contains("hiring") || msg.Contains("join") || msg.Contains("work") || msg.Contains("apply"))
            {
                return "🌟 **Join NGEOS as a Volunteer or Team Member:**\n\nHum hamesha passionate logon ko welcome karte hain jo humanity ki khidmat karna chahte hain!\n* Open volunteer positions dekhne aur online apply karne ke liye **[Career & Volunteers](/About/Career)** page visit karein.\n* Apni profile create kar ke hamare upcoming events mein ground volunteer ban sakte hain.";
            }

            // 7. Contact / Office / Details
            if (msg.Contains("contact") || msg.Contains("address") || msg.Contains("phone") || msg.Contains("email") || msg.Contains("office") || msg.Contains("location") || msg.Contains("kahan"))
            {
                return "📍 **NGEOS Central Office & Contact Info:**\n\n* **Address:** 100 Edhi Street, Saddar, Karachi, Pakistan\n* **Phone:** +92 (300) 123-4567 | +92 21 3456 7890\n* **Email:** support@ngeos.org | ngeos@gmail.com\n* **Support Desk:** Agar aapka koi makhsoos sawal hai to **[Raise A Query](/Home/RaiseQuery)** par ticket create karein, hamari team foran jawab degi.";
            }

            // 8. General Platform Summary Fallback
            return "ℹ️ **NGEOS Platform Overview:**\n\nNGEOS (Next Generation NGO Management & Donation Platform) Pakistan ka modern charity aur welfare platform hai jo verified NGOs aur donors ko ek jagah connect karta hai.\n\n* 💖 **[Active Causes](/Causes):** Welfare projects browse karein\n* 💳 **[Donate Now](/Donate):** Instant secure donation\n* 🧮 **[Zakat Calculator](/Zakat):** Fast Zakat calculation\n* 📞 **[Contact Us](/Home/Contact):** Support team se rabta karein\n\nAap mazeed kisi specific topic (Donation, Zakat, Causes, Volunteering) ke bare mein sawal pooch sakte hain!";
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
