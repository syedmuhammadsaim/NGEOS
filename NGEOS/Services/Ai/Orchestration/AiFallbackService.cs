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

        private bool IsUrduScript(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text)
            {
                if (c >= '\u0600' && c <= '\u06FF') return true;
            }
            return false;
        }

        private bool IsRomanUrdu(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var keywords = new[] { "aap", "kya", "kaise", "kese", "hai", "hain", "mujhe", "mera", "meri", "hum", "batao", "shukriya", "madad", "chahiye", "karna", "paisa", "kitna", "kon", "kis", "kar", "apna", "apni", "hoga", "hogi", "bhi", "yeh", "woh", "sath" };
            var words = text.ToLowerInvariant().Split(new[] { ' ', '?', '!', ',', '.', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var w in words)
            {
                if (Array.IndexOf(keywords, w) >= 0) return true;
            }
            return false;
        }

        private string GenerateGroundedKnowledgeReply(string userMessage, string liveContext)
        {
            var msg = (userMessage ?? string.Empty).ToLowerInvariant().Trim();
            bool isUrdu = IsUrduScript(userMessage ?? string.Empty);
            bool isRoman = IsRomanUrdu(userMessage ?? string.Empty);

            // 1. Greetings
            if (msg == "hi" || msg == "hello" || msg == "hey" || msg.Contains("salam") || msg.Contains("assalam") || msg == "aoa")
            {
                if (isUrdu)
                {
                    return "**السلام علیکم! NGEOS میں خوش آمدید۔**\n\nمیں NGEOS کا تصدیق شدہ AI اسسٹنٹ ہوں۔ میں آپ کی کس طرح مدد کر سکتا ہوں؟\n\n* **[مہمات](/Causes):** فلاحی منصوبے دیکھیں\n* **[عطیہ](/Donate):** آن لائن عطیہ یا زکوٰۃ ادا کریں\n* **[زکوٰۃ کیلکولیٹر](/Zakat):** شرعی حساب لگائیں\n* **[پروگرامز](/Programmes):** فلاحی سرگرمیوں میں حصہ لیں";
                }
                if (isRoman)
                {
                    return "**Assalam-o-Alaikum! Welcome to NGEOS.**\n\nMain NGEOS ka verified AI Assistant hoon. Main aapki kis tarah madad kar sakta hoon?\n\n* **[Causes](/Causes):** Active charity aur welfare campaigns browse karein\n* **[Donate](/Donate):** Online donation ya Zakat ada karein\n* **[Zakat Calculator](/Zakat):** Apni Zakat ka shari'i hisab karein\n* **[Programmes](/Programmes):** Community aur volunteer drives mein hissa lein\n\nAap apna sawal yahan type kar sakte hain!";
                }

                return "**Assalam-o-Alaikum! Welcome to NGEOS.**\n\nI am your verified NGEOS AI Assistant. How may I assist you today?\n\n* **[Browse Causes](/Causes):** Explore verified active charity campaigns\n* **[Make a Donation](/Donate):** Instant online donation & Zakat\n* **[Smart Zakat Calculator](/Zakat):** Calculate your obligatory Zakat according to Shariah\n* **[Programmes](/Programmes):** Participate in community and volunteer initiatives\n\nFeel free to ask any question!";
            }

            // 2. Donation & Payments
            if (msg.Contains("donate") || msg.Contains("donation") || msg.Contains("pay") || msg.Contains("payment") || msg.Contains("card") || msg.Contains("paisa") || msg.Contains("kaise") || msg.Contains("kese"))
            {
                if (isUrdu)
                {
                    return "**NGEOS پر عطیہ دینا انتہائی آسان اور 100% محفوظ ہے:**\n\n1. **[مہمات کا صفحہ](/Causes)** وزٹ کر کے اپنی پسندیدہ مہم منتخب کریں یا براہ راست **[ابھی عطیہ دیں](/Donate)** پر کلک کریں۔\n2. رقم درج کریں (زکوٰۃ یا عام عطیہ)۔\n3. ادائیگی کا طریقہ منتخب کریں (کارڈ، سیف پے، جاز کیش، ایزی پیسہ)۔\n4. ادائیگی مکمل ہونے پر فوری **آفیشل ٹیکس فری رسید** حاصل کریں۔";
                }
                if (isRoman)
                {
                    return "**NGEOS par Donation dena intehayi aasan aur 100% secure hai:**\n\n1. **[Causes Page](/Causes)** par ja kar apni pasand ki cause select karein ya direct **[Donate Now](/Donate)** click karein.\n2. Apni donation amount enter karein (Zakat ya General).\n3. Payment method chunein (Debit/Credit Card, Safepay, JazzCash, Easypaisa).\n4. Payment mukammal hote hi aapko foran **Official Tax-Exempt PDF Receipt** mil jayegi jo aap download ya print kar sakte hain.\n\nKisi bhi madad ke liye aap hamare **[Help Centre](/HelpCentre)** par bhi rabta kar sakte hain.";
                }

                return "**Making a donation on NGEOS is simple, transparent, and 100% secure:**\n\n1. Visit the **[Causes Page](/Causes)** to select an initiative or click **[Donate Now](/Donate)** directly.\n2. Enter your donation amount (General Donation or Zakat).\n3. Choose your payment method (Debit/Credit Card, Safepay, JazzCash, Easypaisa).\n4. Upon completion, you will immediately receive an **Official Tax-Exempt PDF Receipt** available for download.\n\nFor any questions, visit our **[Help Centre](/HelpCentre)**.";
            }

            // 3. Causes & Campaigns
            if (msg.Contains("cause") || msg.Contains("campaign") || msg.Contains("education") || msg.Contains("water") || msg.Contains("health") || msg.Contains("medical") || msg.Contains("orphan") || msg.Contains("relief"))
            {
                if (isUrdu)
                {
                    return "**NGEOS کی فعال فلاحی مہمات:**\n\n* **تعلیم برائے سب:** مستحق بچوں کے لیے اسکول بیگز، یونیفارم اور کتب۔\n* **صاف پانی:** سندھ اور بلوچستان میں سولر واٹر پلانٹس اور ہینڈ پمپس۔\n* **ہنگامی طبی امداد:** نادار مریضوں کے لیے مفت ادویات اور علاج۔\n* **خواتین کی خود مختاری:** ووکیشنل ٹریننگ اور روزگار سپورٹ۔\n\nتفصیلات کے لیے **[Causes](/Causes)** کا صفحہ دیکھیں۔";
                }
                if (isRoman)
                {
                    return "**NGEOS Active Welfare Causes:**\n\nHumari verified campaigns jinme aap hissa le sakte hain:\n* **Universal Education:** Underprivileged bachon ke liye school bags, books aur uniform support.\n* **Clean Drinking Water:** Sindh aur Balochistan mein solar water filtration plants aur handpumps.\n* **Emergency Medical Aid:** Zarooratmand mareezon ke liye free medicines aur diagnostic care.\n* **Women Empowerment:** Hunarmand aurat project ke tehat vocational training.\n\nTamam causes ki tafseelat aur progress dekhne ke liye **[Causes](/Causes)** page visit karein.";
                }

                return "**NGEOS Active Welfare Causes:**\n\nOur verified causes actively supporting vulnerable communities:\n* **Universal Education:** School kits, books, and uniforms for underprivileged children.\n* **Clean Drinking Water:** Solar-powered water filtration plants and wells.\n* **Emergency Medical Aid:** Free diagnostic care, medicines, and medical relief camps.\n* **Women Empowerment:** Vocational skills training and micro-enterprise kits.\n\nExplore all causes and live progress on our **[Causes](/Causes)** page.";
            }

            // 4. Zakat
            if (msg.Contains("zakat") || msg.Contains("sadqah") || msg.Contains("fitrah") || msg.Contains("nisab"))
            {
                if (isUrdu)
                {
                    return "**NGEOS سمارٹ زکوٰۃ مینجمنٹ:**\n\n* ہمارا **[Smart Zakat Calculator](/Zakat)** سونا، چاندی، کیش اور اثاثوں پر 2.5% زکوٰۃ کا شرعی حساب لگاتا ہے۔\n* NGEOS پر زکوٰۃ صرف شرعی طور پر تصدیق شدہ مستحقین تک پہنچائی جاتی ہے۔\n* ہر زکوٰۃ پر شفاف آڈٹ رپورٹ فراہم کی جاتی ہے۔";
                }
                if (isRoman)
                {
                    return "**Smart Zakat Management on NGEOS:**\n\n* Hamare paas **[Smart Zakat Calculator](/Zakat)** mojood hai jo Gold, Silver, Cash aur Assets par automatically 2.5% Zakat compute karta hai.\n* NGEOS par sirf un certified causes ko Zakat di ja sakti hai jo Shariah-compliant aur verified Mustahiqeen tak pohanchti hain.\n* Har Zakat donation par verified tracking aur transparent report generate hoti hai.";
                }

                return "**Smart Zakat Management on NGEOS:**\n\n* Use our **[Smart Zakat Calculator](/Zakat)** to accurately compute 2.5% Zakat across Gold, Silver, Cash, and Assets.\n* NGEOS strictly channels Zakat donations to Shariah-verified eligible beneficiaries (Mustahiqeen).\n* Every contribution is backed by end-to-end transparent reporting and tracking.";
            }

            // 5. Default General Overview
            if (isUrdu)
            {
                return "**NGEOS پلیٹ فارم کا جائزہ:**\n\nNGEOS پاکستان کا معروف خیراتی اور فلاحی نیٹ ورک ہے جو شفاف انداز میں تصدیق شدہ فلاحی اداروں کو مخیر حضرات سے جوڑتا ہے۔\n\n* **[فعال مہمات](/Causes):** فلاحی منصوبے دیکھیں\n* **[عطیہ دیں](/Donate):** فوری اور محفوظ عطیہ\n* **[زکوٰۃ کیلکولیٹر](/Zakat):** آسان شرعی حساب\n* **[رابطہ](/Home/Contact):** سپورٹ ٹیم سے رابطہ کریں";
            }
            if (isRoman)
            {
                return "**NGEOS Platform Overview:**\n\nNGEOS (Next Generation NGO Management & Donation Platform) Pakistan ka modern charity aur welfare platform hai jo verified NGOs aur donors ko ek jagah connect karta hai.\n\n* **[Active Causes](/Causes):** Welfare projects browse karein\n* **[Donate Now](/Donate):** Instant secure donation\n* **[Zakat Calculator](/Zakat):** Fast Zakat calculation\n* **[Contact Us](/Home/Contact):** Support team se rabta karein\n\nAap mazeed kisi specific topic ke bare mein sawal pooch sakte hain!";
            }

            return "**NGEOS Platform Overview:**\n\nNGEOS (Next Generation NGO Management & Donation Platform) is a premier philanthropic network connecting accredited charities, humanitarian campaigns, and benefactors worldwide.\n\n* **[Active Causes](/Causes):** Browse verified projects\n* **[Donate Now](/Donate):** Instant and secure donations\n* **[Zakat Calculator](/Zakat):** Calculate your obligatory Zakat\n* **[Contact Us](/Home/Contact):** Connect with our administration\n\nFeel free to ask any question in English, Roman Urdu, or Urdu!";
        }

        private string BuildSystemPrompt(string liveContext, string? userRole)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are the official AI Assistant for NGEOS (Next Generation NGO Management & Donation Platform).");
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
            sb.AppendLine("5. STRICT LANGUAGE MIRRORING: You MUST detect the language of the user's message and reply in that EXACT same language:\n- If the user writes in English, you MUST reply 100% in English.\n- If the user writes in Roman Urdu (Urdu written in Latin letters, e.g. 'Donation kaise karein?', 'Total kitna paisa aya hai?'), you MUST reply 100% in natural, polite Roman Urdu.\n- If the user writes in Urdu script (e.g. 'عطیہ کیسے دیں؟'), you MUST reply 100% in Urdu script.\nNEVER answer in Roman Urdu if the user asked in English, and NEVER answer in English if the user asked in Roman Urdu or Urdu!");
            sb.AppendLine("6. FORMATTING: Use clean markdown with clear headings, bullet points, and bold text for readability. Avoid decorative emojis or cartoonish icons; maintain a clean, professional, executive presentation.");
            sb.AppendLine("7. COMPLETE A-TO-Z ACTIONABLE GUIDANCE: Provide clear, end-to-end guidance for all user inquiries across the entire platform:\n- Accounts & Security: Registration (/Account/Register), Login (/Account/Login), Password Reset (/Account/ForgotPassword).\n- Donation Process: Step-by-step guidance on selecting causes (/Causes), choosing amounts and donation types (General, Zakat, Sadqah, Fitrah), and obtaining verified digital receipts (/User/Donations).\n- Zakat Calculation: Islamic computation at 2.5% on assets above Nisab (/Zakat).\n- Volunteering & Careers: Joining field drives or applying for openings (/About/Career).\n- Programmes & Events: Exploring community initiatives (/Programmes) and charity events (/Events).\n- Accredited NGOs: Browsing partner organizations (/Ngos) and partnership inquiries (/Home/Contact).\n- Support Desk: Submitting user inquiries (/User/Queries or /Home/Contact).");

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
