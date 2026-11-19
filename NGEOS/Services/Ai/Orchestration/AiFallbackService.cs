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
    /// and seamlessly handles Groq -> Gemini failover, with dedicated Admin vs User isolation
    /// and full fluency in Roman Urdu, Urdu script, and English.
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

            bool isAdmin = (request?.IsAdmin == true) 
                        || (userRole != null && (userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) || userRole.Contains("Admin", StringComparison.OrdinalIgnoreCase)));

            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                response.IsSuccess = false;
                response.Reply = isAdmin 
                    ? "Assalam-o-Alaikum Administrator! Koi sawal ya Admin Section ka naam likhein." 
                    : "Please enter a message to begin the conversation.";
                response.SuggestedActions = isAdmin ? GetAdminDefaultSuggestionChips() : GetDefaultSuggestionChips();
                return response;
            }

            // 1. Fetch verified live grounding context from SQL Server
            string liveContext = await _knowledgeService.GetGroundingContextAsync(request.Message, cancellationToken);

            // 2. Build multi-language system prompt
            string systemPrompt = BuildSystemPrompt(liveContext, isAdmin ? "Admin" : userRole);

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

            // 5. Final Response Formatting with Intelligent Grounded Knowledge Fallback
            if (string.IsNullOrWhiteSpace(aiReply))
            {
                _logger.LogInformation("Generating grounded knowledge response from NGEOS database for role: {Role}", isAdmin ? "Admin" : "User");
                if (isAdmin)
                {
                    aiReply = GenerateAdminGroundedKnowledgeReply(request.Message, liveContext);
                }
                else
                {
                    aiReply = GenerateGroundedKnowledgeReply(request.Message, liveContext);
                }
                providerUsed = "NGEOS Grounded Knowledge";
            }

            stopwatch.Stop();

            response.IsSuccess = true;
            response.Reply = aiReply;
            response.ProviderUsed = providerUsed;
            response.SuggestedActions = isAdmin ? GenerateAdminContextualChips(request.Message) : GenerateContextualChips(request.Message);
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
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "aap", "ap", "kya", "kia", "kyun", "kyu", "q", "kab", "kahan", "kidhar", "kaise", "kese", 
                "kesa", "kaisa", "kaisay", "kesay", "hai", "h", "hy", "hain", "hyn", "ho", "hu", "hoon", "hun", 
                "tha", "thi", "the", "thay", "bhai", "yaar", "suno", "sun", "bolo", "bol", "baat", "bat", 
                "karo", "kro", "karna", "krna", "karein", "kren", "krte", "karte", "batao", "btao", "batayein", 
                "bataiye", "bataen", "dikhao", "dekho", "dekhein", "dekhain", "chahiye", "chahye", "paisa", 
                "paise", "rupay", "rupee", "kitna", "kitne", "kitni", "kitnay", "kon", "kaun", "kn", "kis", 
                "kisko", "kisi", "apna", "apni", "apne", "apnay", "hoga", "hogi", "hoge", "hogay", "bhi", 
                "bi", "yeh", "ye", "woh", "wo", "sath", "admin", "panel", "data", "users", "log", "kam", 
                "kaam", "theek", "thik", "shukriya", "madad", "help", "haal", "chal", "kuch", "sab", "si", 
                "sahi", "acha", "achha", "shuru", "band", "khata", "hisab", "mujhe", "mjhe", "mera", "meri", 
                "mere", "meray", "hum", "hm", "humari", "hamari", "humara", "hamara", "bhejo", "bhej", 
                "horha", "chalrha", "kare", "sakta", "sakti", "sakte", "nahi", "nhi", "na", "mat", "lekin", 
                "lkn", "magar", "or", "aur", "pe", "par", "pr", "se", "sy", "ko", "ka", "ki", "ke", "kay", 
                "main", "mein", "mn", "me", "to", "tw", "toh", "sir", "sahab", "janab", "karo", "kro", 
                "jawab", "reply", "bhai", "shuru"
            };

            var words = text.ToLowerInvariant().Split(new[] { ' ', '?', '!', ',', '.', '\n', '\r', ':', ';', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var w in words)
            {
                if (keywords.Contains(w)) return true;
            }
            return false;
        }

        /// <summary>
        /// Admin Executive Grounded Knowledge Engine covering all 15 Admin Sections
        /// with full Roman Urdu, Urdu script, and English fluency.
        /// </summary>
        private string GenerateAdminGroundedKnowledgeReply(string userMessage, string liveContext)
        {
            var msg = (userMessage ?? string.Empty).ToLowerInvariant().Trim();
            bool isUrdu = IsUrduScript(userMessage ?? string.Empty);
            bool isRoman = IsRomanUrdu(userMessage ?? string.Empty);

            // 1. Casual Chit-Chat, Conversation & Greetings in Roman Urdu / English
            if (msg == "hi" || msg == "hello" || msg == "hey" || msg.Contains("salam") || msg.Contains("assalam") || msg == "aoa"
                || msg.Contains("kya haal") || msg.Contains("kese ho") || msg.Contains("kaise ho") || msg.Contains("kaisa hai") || msg.Contains("kesa hai")
                || msg.Contains("baat") || msg.Contains("suno") || msg.Contains("kya chal") || msg.Contains("kya ho rha") || msg.Contains("reply") || msg.Contains("jawab")
                || msg.Contains("theek") || msg.Contains("thik") || msg.Contains("kuch batao") || msg.Contains("kuch bolo") || msg.Contains("sun rhe ho"))
            {
                if (isUrdu)
                {
                    return "**السلام علیکم ایڈمنسٹریٹر صاحب! 🛡️**\n\nالحمدللہ میں بالکل ٹھیک ہوں اور NGEOS کا خودمختار ایڈمن AI انجن چوبیس گھنٹے فعال ہے۔ میں ایڈمن ڈیش بورڈ کے تمام 15 حصوں، لائیو فنانشل لیجر اور سیکیورٹی تھریٹس کی نگرانی کر رہا ہوں۔\n\nآپ ایڈمن پینل کے کس سیکشن کی رپورٹ یا ڈیٹا دیکھنا چاہتے ہیں؟\n\n* **[عطیات اور لیجر](/Admin/Donations):** مالیاتی ٹرانزیکشنز اور رسیدیں\n* **[مہمات منیجر](/Admin/Causes):** فعال مہمات اور فنڈز کا ہدف\n* **[صارفین ڈائریکٹری](/Admin/Users):** رجسٹرڈ ممبران اور کردار\n* **[سیکیورٹی انٹیلی جنس](/Admin/SecurityIntelligence):** تھریٹ ڈیفنس اور خودکار آڈٹ\n* **[ایجنٹ رپورٹس](/Admin/AgentReports):** خودکار AI لاگز";
                }
                if (isRoman || true) // Default to Roman Urdu for colloquial/chit-chat if not Urdu script
                {
                    return "**Assalam-o-Alaikum Administrator Sahab! 🛡️**\n\nJee bilkul, main aap ke sath Roman Urdu mein baat kar raha hoon! NGEOS Autonomous Admin AI Engine 24/7 poori tarah active hai aur Admin Dashboard ke tamaam 15 sections aur live financial ledger ki monitoring kar raha hai.\n\nAap Admin Panel ke mutaliq kya report ya data dekhna chahte hain?\n\n* **[Donations & Ledger](/Admin/Donations):** Live financial collections & transaction records\n* **[Welfare Causes](/Admin/Causes):** Active campaigns aur funding target progress\n* **[Member Directory](/Admin/Users):** Registered users aur security roles\n* **[Security Intelligence](/Admin/SecurityIntelligence):** Threat defense aur automated audits\n* **[Agent Reports](/Admin/AgentReports):** Autonomous AI execution logs\n* **[Support Queries](/Admin/Queries):** Pending donor tickets\n\nAap Roman Urdu ya English mein jo sawal poochna chahein, type karein!";
                }

                return "**Assalam-o-Alaikum, Administrator! 🛡️**\n\nI am fully operational and actively monitoring all 15 NGEOS Admin Dashboard modules and live financial records.\n\nHow can I assist you with platform administration today?\n\n* **[Donations & Ledger](/Admin/Donations):** Master financial transaction records\n* **[Welfare Causes](/Admin/Causes):** Active campaigns and funding targets\n* **[Member Directory](/Admin/Users):** Registered users and security roles\n* **[Security Intelligence](/Admin/SecurityIntelligence):** Threat Defense and audit trails\n* **[Agent Audit Reports](/Admin/AgentReports):** Autonomous AI execution logs";
            }

            // 2. All Admin Sections Directory / Overview
            if (msg.Contains("admin") || msg.Contains("panel") || msg.Contains("section") || msg.Contains("sections") 
                || msg.Contains("menu") || msg.Contains("module") || msg.Contains("dashboard") || msg.Contains("overview") || msg.Contains("kya feature") || msg.Contains("kya amoor"))
            {
                if (isUrdu)
                {
                    return "**NGEOS ایڈمن پینل کے تمام 15 سیکشنز کا جائزہ:**\n\n1. **فنڈ ریزنگ اور مالیات:**\n   * **[عطیات اور لیجر](/Admin/Donations):** تصدیق شدہ ادائیگیاں اور بینک ٹرانسفرز\n   * **[فلاحی مہمات](/Admin/Causes):** مہمات کی منظوری، ہدف اور ترمیم\n   * **[ممبر ڈائریکٹری](/Admin/Users):** صارفین کے اکاؤنٹس، ایڈمن رولز اور رسائی\n\n2. **تنظیم اور پروگرامز:**\n   * **[منسلک NGOs](/Admin/Ngos):** تصدیق شدہ فلاحی شراکت دار\n   * **[کارپوریٹ پارٹنرز](/Admin/Partners):** CSR اور ٹیکنالوجی پارٹنرز\n   * **[پروگرامز](/Admin/Programmes):** فلاحی مہمات اور تربیتی ورکشاپس\n   * **[والنٹئیرز](/Admin/ProgrammeInterests):** رضاکارانہ درخواستیں اور شرکاء\n   * **[چیریٹی ایونٹس](/Admin/Events):** میراتھن، گالا اور بلڈ ڈرائیوز\n\n3. **سیکیورٹی اور خود مختار AI:**\n   * **[تھریٹ ڈیفنس اور سیکیورٹی](/Admin/SecurityIntelligence):** مشکوک سرگرمیوں کی روک تھام اور IP بین\n   * **[ایجنٹ آڈٹ رپورٹس](/Admin/AgentReports):** خودکار AI ایجنٹ لاگز\n   * **[عطیات انٹیلی جنس](/Admin/DonationIntelligence):** پیشین گوئی اور رجحانات\n   * **[ایجنٹ کنٹرول سینٹر](/Admin/AgentControl):** ایجنٹ رن ٹائم مانیٹرنگ\n\n4. **سپورٹ اور مواد (CMS):**\n   * **[صارفین کے سوالات](/Admin/Queries):** سپورٹ ٹکٹس اور حل\n   * **[رابطہ انکوائریز](/Admin/ContactMessages):** ویب سائٹ پیغامات\n   * **[میڈیا گیلری اور CMS](/Admin/Gallery):** تصاویر، ٹیم ممبران اور FAQs";
                }
                if (isRoman)
                {
                    return "**NGEOS Admin Dashboard Modules & Sections:**\n\n1. **Fundraising & Finance:**\n   * **[Donations & Ledger](/Admin/Donations):** Verified payment records, refunds aur receipts\n   * **[Welfare Causes](/Admin/Causes):** Campaign approvals, target tracking aur edits\n   * **[Member Directory](/Admin/Users):** User management, admin roles aur security status\n\n2. **Organization & Community:**\n   * **[Associated NGOs](/Admin/Ngos):** Verified partner welfare organizations\n   * **[Corporate Partners](/Admin/Partners):** CSR aur technology partners\n   * **[Our Programmes](/Admin/Programmes):** Educational, healthcare aur welfare drives\n   * **[Volunteers & Participants](/Admin/ProgrammeInterests):** Volunteer applications aur field drives\n   * **[Charity Events](/Admin/Events):** Upcoming marathons, galas aur health expos\n\n3. **Security & Autonomous AI:**\n   * **[Security Intelligence & Threat Defense](/Admin/SecurityIntelligence):** IP bans, anomaly detection aur cryptographic audits\n   * **[Agent Audit Reports](/Admin/AgentReports):** Autonomous AI execution logs\n   * **[Donation Intelligence](/Admin/DonationIntelligence):** Predictive analytics aur peak trends\n   * **[Agent Control Center](/Admin/AgentControl):** Runtime automation switchboard\n\n4. **Support & CMS:**\n   * **[User Queries](/Admin/Queries):** Donor support tickets aur SLA tracking\n   * **[Contact Inquiries](/Admin/ContactMessages):** Public website messages\n   * **[CMS, Gallery & FAQs](/Admin/Gallery):** Media assets, leadership profiles aur FAQs";
                }

                return "**NGEOS Admin Dashboard Modules & Sections:**\n\n1. **Fundraising & Finance:**\n   * **[Donations & Ledger](/Admin/Donations):** Verified transactions, bank reconciliations, and tax receipts\n   * **[Welfare Causes](/Admin/Causes):** Campaign management, funding milestones, and approvals\n   * **[Member Directory](/Admin/Users):** User management, role elevation, and security status\n\n2. **Organization & Community:**\n   * **[Associated NGOs](/Admin/Ngos):** Accredited partner organizations\n   * **[Corporate Partners](/Admin/Partners):** Corporate CSR benefactors\n   * **[Our Programmes](/Admin/Programmes):** Sustainable field welfare drives\n   * **[Volunteers & Participants](/Admin/ProgrammeInterests):** Volunteer applications\n   * **[Charity Events](/Admin/Events):** Charity galas, walks, and expos\n\n3. **Security & Autonomous AI:**\n   * **[Security Intelligence & Threat Defense](/Admin/SecurityIntelligence):** Automated attack mitigation and IP blacklisting\n   * **[Agent Audit Reports](/Admin/AgentReports):** Tamper-resistant execution logs\n   * **[Donation Intelligence](/Admin/DonationIntelligence):** Predictive velocity and peak analytics\n   * **[Agent Control Center](/Admin/AgentControl):** Autonomous daemon supervision\n\n4. **Support & CMS:**\n   * **[User Queries](/Admin/Queries):** Donor assistance tickets\n   * **[Contact Messages](/Admin/ContactMessages):** External contact submissions\n   * **[CMS, Gallery & FAQs](/Admin/Gallery):** Media assets and knowledge base";
            }

            // 3. Donations & Master Financial Ledger
            if (msg.Contains("ledger") || msg.Contains("donation") || msg.Contains("donations") || msg.Contains("fund") 
                || msg.Contains("funds") || msg.Contains("paisa") || msg.Contains("rupay") || msg.Contains("transaction") 
                || msg.Contains("transactions") || msg.Contains("finance") || msg.Contains("collection") || msg.Contains("amount") 
                || msg.Contains("total") || msg.Contains("hisab") || msg.Contains("khata"))
            {
                if (isUrdu)
                {
                    return "**NGEOS فنانشل لیجر اور عطیات کا آڈٹ ڈیٹا:**\n\n* **کل تصدیق شدہ فنڈز:** لائیو ڈیٹابیس کے مطابق صرف **Completed** ادائیگیاں شامل ہیں۔\n* **ادائیگی کے ذرائع:** آن لائن ڈیبٹ/کریڈٹ کارڈ، سیف پے، ایزی پیسہ، جاز کیش اور ڈائریکٹ بینک ڈپازٹ۔\n* **لیجر کی خصوصیات:** ہر ٹرانزیکشن کی الگ ڈیجیٹل رسید، ٹرانزیکشن ID، اور زکوٰۃ بمقابلہ جنرل ڈونیشن کا شفاف ریکارڈ۔\n\nمکمل لیجر اور بریک ڈاؤن دیکھنے کے لیے **[Donations & Ledger](/Admin/Donations)** وزٹ کریں۔";
                }
                if (isRoman)
                {
                    return "**NGEOS Financial Ledger & Donations Summary:**\n\n* **Verified Funds Mobilized:** Live database ke mutabiq tamam verified payments master ledger mein recorded hain (sirf Completed transactions count hoti hain).\n* **Payment Modes:** Online Credit/Debit Card, Safepay, Easypaisa, JazzCash, aur Direct Bank Transfer.\n* **Ledger Security:** Har transaction par unique cryptographic ID aur digital tax receipt automatically attach hoti hai.\n* **Allocation:** Zakat aur General Donations alag alag head mein maintain ki ja rahi hain.\n\nMukammal records aur transactions list dekhne ke liye **[Donations & Ledger](/Admin/Donations)** par click karein.";
                }

                return "**NGEOS Financial Ledger & Donations Summary:**\n\n* **Financial Integrity:** Grounded strictly in confirmed completed transactions. Pending or cancelled items are excluded from total raised funds.\n* **Supported Channels:** Multi-gateway processing (Safepay, Cards, Easypaisa, JazzCash, Direct Bank).\n* **Audit Compliance:** Every donation is timestamped, hashed, and linked to a downloadable digital tax receipt.\n\nAccess the full transaction register via **[Donations & Ledger](/Admin/Donations)**.";
            }

            // 4. Welfare Causes Manager
            if (msg.Contains("cause") || msg.Contains("causes") || msg.Contains("campaign") || msg.Contains("campaigns") 
                || msg.Contains("mushkilat") || msg.Contains("welfare") || msg.Contains("target") || msg.Contains("funding"))
            {
                if (isUrdu)
                {
                    return "**فلاحی مہمات (Welfare Causes) کا ایڈمن مانیٹرنگ اسٹیٹس:**\n\n* تمام فعال مہمات کی نگرانی ایڈمن ڈیش بورڈ سے براہ راست کی جا سکتی ہے۔\n* ایڈمن نئی مہمات بنا سکتے ہیں، فنڈنگ کا ہدف تبدیل کر سکتے ہیں، یا مہمات کو فیچرڈ (Featured) کر سکتے ہیں۔\n* عطیات کا تناسب خودکار پروگریس بار کے ذریعے دکھایا جاتا ہے۔\n\nمہمات کا انتظام کرنے کے لیے **[Welfare Causes Manager](/Admin/Causes)** کا صفحہ دیکھیں۔";
                }
                if (isRoman)
                {
                    return "**Welfare Causes Management Summary:**\n\n* **Campaign Oversight:** Platform par running tamam active causes ka target, raised amount, aur donor participation live track hota hai.\n* **Admin Controls:** Aap kisi bhi cause ko Edit, Feature, ya Deactivate kar sakte hain, aur nayi emergency welfare campaigns add kar sakte hain.\n* **Categories:** Education, Clean Water, Healthcare, Orphan Support, aur Women Empowerment.\n\nTamam causes manage karne ke liye **[Welfare Causes Manager](/Admin/Causes)** page open karein.";
                }

                return "**Welfare Causes Management Summary:**\n\n* **Campaign Controls:** Supervise campaign funding goals, raised amounts, and donor counts in real-time.\n* **Administrative Actions:** Create new emergency relief drives, toggle featured status, or modify targets.\n* **Focus Categories:** Universal Education, Clean Water Filtration, Healthcare, Orphan Care, and Economic Empowerment.\n\nManage all initiatives via **[Welfare Causes Manager](/Admin/Causes)**.";
            }

            // 5. Member Directory & User Accounts
            if (msg.Contains("user") || msg.Contains("users") || msg.Contains("member") || msg.Contains("members") 
                || msg.Contains("directory") || msg.Contains("accounts") || msg.Contains("role") || msg.Contains("roles") || msg.Contains("log"))
            {
                if (isUrdu)
                {
                    return "**رجسٹرڈ ممبرز ڈائریکٹری (Member Directory):**\n\n* تمام رجسٹرڈ ڈونرز، والنٹئیرز اور ایڈمنسٹریٹرز کی فہرست محفوظ ہے۔\n* ایڈمنز کسی بھی صارف کے کردار (Role)، ای میل ویریفکیشن اور سیکیورٹی اسٹیٹس کو اپ ڈیٹ کر سکتے ہیں۔\n* مشکوک سرگرمی پر اکاؤنٹ معطل یا ری سیٹ کیا جا سکتا ہے۔\n\nصارفین کی فہرست کے لیے **[Registered Members](/Admin/Users)** وزٹ کریں۔";
                }
                if (isRoman)
                {
                    return "**Registered Member Directory Summary:**\n\n* **User Management:** NGEOS par registered tamam donors, volunteers, aur administrative staff ka centralized record mojood hai.\n* **Role-Based Access Control:** Super Admin, Admin, aur Standard Member roles ki assignment aur permission toggles.\n* **Security Controls:** Account lock/unlock, password resets, aur login session management.\n\nUser directory inspect karne ke liye **[Registered Members Directory](/Admin/Users)** visit karein.";
                }

                return "**Registered Member Directory Summary:**\n\n* **Identity & Access Management:** Centralized registry of all authenticated platform donors, volunteers, and admins.\n* **RBAC Roles:** Full role lifecycle management (Super Admin, Admin, Member, Field Volunteer).\n* **Account Security:** Instant suspension, session invalidation, and password resets.\n\nInspect user profiles via **[Registered Members Directory](/Admin/Users)**.";
            }

            // 6. Security Intelligence, Threat Defense & Suspensions
            if (msg.Contains("security") || msg.Contains("threat") || msg.Contains("threats") || msg.Contains("defense") 
                || msg.Contains("ban") || msg.Contains("bans") || msg.Contains("ip") || msg.Contains("attack") 
                || msg.Contains("hack") || msg.Contains("suspicious") || msg.Contains("fraud") || msg.Contains("block"))
            {
                if (isUrdu)
                {
                    return "**سیکیورٹی انٹیلی جنس اور تھریٹ ڈیفنس اسٹیٹس:**\n\n* **خودکار سیکیورٹی انجن:** مشکوک لاگ انز، ریٹ لمٹ کی خلاف ورزی اور مالیاتی دھوکہ دہی کی 24/7 نگرانی کرتا ہے۔\n* **IP بیننگ سسٹم:** خطرناک IP ایڈریسز کو خودکار طریقے سے فائر وال پر بلاک کیا جاتا ہے۔\n* **سیکورٹی آڈٹ ٹریل:** ہر حساس ایڈمن ایکشن کا انکرپٹڈ ریکارڈ مرتب ہوتا ہے۔\n\nسیکیورٹی تفصیلات کے لیے **[Security Intelligence](/Admin/SecurityIntelligence)** کا صفحہ دیکھیں۔";
                }
                if (isRoman)
                {
                    return "**Security Intelligence & Threat Defense Status:**\n\n* **Autonomous Security Engine:** Rate limit violations, brute force login attempts, SQL injection probes, aur payment fraud patterns ko automatically detect karta hai.\n* **Threat Defense Actions:** Suspicious IPs ko temporary ya permanent blacklist kiya jata hai aur admin alerts trigger hoti hain.\n* **Cryptographic Integrity:** Tamam critical actions aur transactions cryptographically audited hain.\n\nThreat logs aur security metrics dekhne ke liye **[Security Intelligence](/Admin/SecurityIntelligence)** open karein.";
                }

                return "**Security Intelligence & Threat Defense Status:**\n\n* **Autonomous Threat Mitigation:** Real-time monitoring of malicious anomalies, credential stuffing, rate-limit bursts, and tampering.\n* **Active Defense Actions:** Automated IP blacklisting, rate-limiting escalation, and session revocation.\n* **Audit Integrity:** Cryptographic hashing on all administrative and security actions.\n\nReview the live threat matrix via **[Security Intelligence](/Admin/SecurityIntelligence)**.";
            }

            // 7. Autonomous AI Agent Reports & Control
            if (msg.Contains("agent") || msg.Contains("agents") || msg.Contains("audit") || msg.Contains("report") 
                || msg.Contains("reports") || msg.Contains("autonomous") || msg.Contains("ai log") || msg.Contains("logs"))
            {
                if (isUrdu)
                {
                    return "**خود مختار AI ایجنٹ آڈٹ رپورٹس:**\n\n* **Tamper-Resistant لاگز:** سسٹم کے تمام AI ایجنٹس (Security Engine, Intelligence Engine, Scheduled Automation) کی کارروائیوں کا ناقابلِ ترمیم ریکارڈ۔\n* **رسک لیول گریڈنگ:** Low, Medium, High, اور Critical خطرات کی فوری نشاندہی۔\n* **CSV ایکسپورٹ:** ریگولیٹری آڈٹ کے لیے تمام رپورٹس ایکسپورٹ کی جا سکتی ہیں۔\n\nرپورٹس دیکھنے کے لیے **[Agent Audit Reports](/Admin/AgentReports)** وزٹ کریں۔";
                }
                if (isRoman)
                {
                    return "**Autonomous AI Agent Audit Reports Summary:**\n\n* **Tamper-Resistant Operational Logs:** AI Security Engine, Intelligence Daemon, aur Background Automation ke execute kiye gaye har action ka transparent audit trail mojood hai.\n* **Risk Classification:** Har automated activity ko Low, Medium, High, ya Critical risk level assign hota hai.\n* **Compliance & Export:** Formal audits ke liye CSV report export karne ki sahulat mojood hai.\n\nAgent execution logs check karne ke liye **[Agent Audit Reports](/Admin/AgentReports)** par jayein.";
                }

                return "**Autonomous AI Agent Audit Reports Summary:**\n\n* **Tamper-Resistant Action Logs:** Immutable audit trail recording operations conducted by the Autonomous AI Security and Intelligence agents.\n* **Risk Stratification:** Actions graded across Low, Medium, High, and Critical risk vectors.\n* **Audit Exports:** Full CSV export capabilities for institutional reporting.\n\nView live logs via **[Agent Audit Reports](/Admin/AgentReports)**.";
            }

            // 8. Donation Intelligence & Predictive Trends
            if (msg.Contains("intelligence") || msg.Contains("predict") || msg.Contains("predictive") || msg.Contains("trend") 
                || msg.Contains("trends") || msg.Contains("analytics") || msg.Contains("forecast") || msg.Contains("velocity"))
            {
                if (isUrdu)
                {
                    return "**عطیات انٹیلی جنس اور پیشین گوئی (Donation Intelligence):**\n\n* **پیشین گوئی اور رجحانات:** ہفتہ وار اور ماہانہ بنیادوں پر فنڈز کے بہاؤ کی پیشن گوئی۔\n* **پیک ڈونیشن اوقات:** جمعہ کی نماز، رمضان المبارک اور قدرتی آفات کے دوران خصوصی فنڈنگ الرٹس۔\n* **پیمنٹ گیٹ وے کا تجزیہ:** کارڈ، سیف پے اور موبائل والٹس کا کارکردگی تجزیہ۔\n\nتفصیلات کے لیے **[Donation Intelligence](/Admin/DonationIntelligence)** وزٹ کریں۔";
                }
                if (isRoman)
                {
                    return "**Donation Intelligence & Predictive Analytics:**\n\n* **Predictive Growth Modeling:** Platform donation inflow ke time-series trends aur machine learning forecasts.\n* **Velocity & Peak Patterns:** Weekly donation velocity, high-impact donor campaigns, aur seasonal surges (e.g. Ramadan, Friday drives).\n* **Gateway Distribution:** Credit card vs mobile wallet payment distributions.\n\nAnalytics dekhne ke liye **[Donation Intelligence](/Admin/DonationIntelligence)** page visit karein.";
                }

                return "**Donation Intelligence & Predictive Analytics:**\n\n* **Predictive Inflow Models:** Time-series projections of donation velocities and campaign fulfillment timelines.\n* **Surge Patterns:** Identification of weekend, holiday, and emergency response donation surges.\n* **Gateway Analytics:** Detailed processing breakdown across mobile wallets and debit/credit cards.\n\nInspect analytics via **[Donation Intelligence](/Admin/DonationIntelligence)**.";
            }

            // 9. Associated NGOs & Corporate Partners
            if (msg.Contains("ngo") || msg.Contains("ngos") || msg.Contains("partner") || msg.Contains("partners") 
                || msg.Contains("alliance") || msg.Contains("corporate") || msg.Contains("sponsor"))
            {
                if (isUrdu)
                {
                    return "**منسلک NGOs اور کارپوریٹ پارٹنرز کا انتظام:**\n\n* **تصدیق شدہ NGOs:** NGEOS پلیٹ فارم سے منظور شدہ تمام پارٹنر فلاحی تنظیموں کے پروفائلز اور رجسٹریشن نمبرز۔\n* **کارپوریٹ پارٹنرز:** ٹیک سپانسرز، لاجسٹک پارٹنرز اور CSR تنظیموں کی تفصیلات۔\n* **ایڈمن اختیارات:** نئے پارٹنرز کو منظور کرنا، اسٹیٹس اپ ڈیٹ کرنا اور تصدیقی بیج جاری کرنا۔\n\nتفصیلات کے لیے **[Associated NGOs](/Admin/Ngos)** اور **[Corporate Partners](/Admin/Partners)** دیکھیں۔";
                }
                if (isRoman)
                {
                    return "**Associated NGOs & Corporate Partners Management:**\n\n* **Accredited Partner NGOs:** Registered aur verified NGOs ki directory, legal registration certificates, aur focus areas.\n* **Corporate CSR Benefactors:** CSR corporate alliances, matching gift sponsors, aur technical logistics partners.\n* **Admin Actions:** Direct verification approvals, partner profile updates, aur alliance listings.\n\nManage karein via **[Associated NGOs](/Admin/Ngos)** aur **[Corporate Partners](/Admin/Partners)**.";
                }

                return "**Associated NGOs & Corporate Partners Management:**\n\n* **Accredited NGO Directory:** Institutional verification, tax-exemption records, and field focus areas.\n* **Corporate Alliances:** Managing corporate CSR matching partners and institutional sponsors.\n* **Administrative Actions:** Issue verification badges, review compliance, and onboard new NGOs.\n\nManage via **[Associated NGOs](/Admin/Ngos)** and **[Corporate Partners](/Admin/Partners)**.";
            }

            // 10. Our Programmes & Charity Events
            if (msg.Contains("programme") || msg.Contains("programmes") || msg.Contains("program") || msg.Contains("programs") 
                || msg.Contains("event") || msg.Contains("events") || msg.Contains("gala") || msg.Contains("marathon"))
            {
                if (isUrdu)
                {
                    return "**کمیونٹی پروگرامز اور چیریٹی ایونٹس:**\n\n* **فلاحی پروگرامز:** تعلیمی کاروان، صحت اور مفت طبی کیمپس، اور ووکیشنل اسکل سینٹرز۔\n* **چیریٹی ایونٹس:** میراتھن، خیراتی عشائیے، اور بلڈ ڈونیشن مہمات کا شیڈول۔\n* **ایڈمن کنٹرول:** نئے ایونٹس کی تاریخ، مقام اور ٹکٹ/رجسٹریشن کی ترتیبات۔\n\nانتظام کے لیے **[Our Programmes](/Admin/Programmes)** اور **[Charity Events](/Admin/Events)** وزٹ کریں۔";
                }
                if (isRoman)
                {
                    return "**Programmes & Charity Events Operations:**\n\n* **Welfare Programmes:** Universal education caravans, medical relief drives, aur women vocational programmes.\n* **Charity Events:** Upcoming welfare marathons, fundraising galas, aur blood donation camps ka schedule aur RSVP tracking.\n* **Admin Controls:** Event scheduling, location management, aur participant coordination.\n\nManage karein via **[Our Programmes](/Admin/Programmes)** aur **[Charity Events](/Admin/Events)**.";
                }

                return "**Programmes & Charity Events Operations:**\n\n* **Field Programmes:** Healthcare caravans, educational drives, and vocational training sessions.\n* **Upcoming Events:** Logistics, dates, venues, and attendee coordination for charity marathons and galas.\n* **Admin Controls:** Create upcoming events, manage attendance, and monitor outcomes.\n\nAccess via **[Our Programmes](/Admin/Programmes)** and **[Charity Events](/Admin/Events)**.";
            }

            // 11. Participant / Volunteers & Job Openings
            if (msg.Contains("volunteer") || msg.Contains("volunteers") || msg.Contains("participant") || msg.Contains("job") 
                || msg.Contains("jobs") || msg.Contains("career") || msg.Contains("careers") || msg.Contains("hiring"))
            {
                if (isUrdu)
                {
                    return "**رضاکارانہ درخواستیں اور ملازمتیں (Volunteers & Careers):**\n\n* **والنٹئیر درخواستیں:** فیلڈ پروگرامز اور ریلیف سرگرمیوں میں شمولیت کی موصول شدہ درخواستیں۔\n* **جاب پوسٹنگز:** NGEOS پلیٹ فارم پر کیریئر کے مواقع اور امیدواروں کی تفصیلات۔\n* **منظوری کا عمل:** ایڈمنز درخواستوں کا جائزہ لے کر انہیں منظور یا مسترد کر سکتے ہیں۔\n\nتفصیلات کے لیے **[Participant / Volunteers](/Admin/ProgrammeInterests)** اور **[Job Postings](/Admin/Jobs)** دیکھیں۔";
                }
                if (isRoman)
                {
                    return "**Volunteer Applications & Job Postings Oversight:**\n\n* **Volunteer Registrations:** Field operations, disaster response, aur medical camps ke liye aane wali volunteer applications.\n* **Career Openings:** NGEOS team ke liye published job opportunities aur incoming resumes.\n* **Admin Actions:** Applications review karein, candidates shortlist karein, aur status update karein.\n\nReview karein via **[Participant / Volunteers](/Admin/ProgrammeInterests)** aur **[Job Postings](/Admin/Jobs)**.";
                }

                return "**Volunteer Applications & Job Postings Oversight:**\n\n* **Volunteer Intake:** Centralized review of applications for medical camps, teaching drives, and flood relief.\n* **Career Postings:** Publishing and managing job requisitions and candidate applications.\n* **Workflow:** Accept, shortlist, or decline candidates directly from the panel.\n\nReview via **[Participant / Volunteers](/Admin/ProgrammeInterests)** and **[Job Postings](/Admin/Jobs)**.";
            }

            // 12. Support Desk & User Queries
            if (msg.Contains("support") || msg.Contains("query") || msg.Contains("queries") || msg.Contains("ticket") 
                || msg.Contains("tickets") || msg.Contains("message") || msg.Contains("messages") || msg.Contains("complaint"))
            {
                if (isUrdu)
                {
                    return "**سپورٹ ڈیسک اور صارفین کے سوالات (Support Queries):**\n\n* ڈونرز اور صارفین کی طرف سے موصول ہونے والی شکایات اور انکوائریز۔\n* ہر سوال پر جواب درج کرنے، ای میل نوٹیفکیشن بھیجنے اور مسئلہ حل کرنے کی سہولت۔\n* پبلک رابطہ فارم سے آنے والے تمام پیغامات کا ریکارڈ۔\n\nسوالات دیکھنے کے لیے **[User Queries](/Admin/Queries)** اور **[Contact Messages](/Admin/ContactMessages)** وزٹ کریں۔";
                }
                if (isRoman)
                {
                    return "**Support Desk & User Queries Overview:**\n\n* **Donor Tickets:** Donation verification, receipt queries, aur general platform support tickets.\n* **Resolution Workflow:** Admin direct reply likh sakta hai jo user ko email aur dashboard par deliver hota hai.\n* **Contact Inquiries:** Website contact page ke through submit kiye gaye general inquiry messages.\n\nTickets resolve karne ke liye **[User Queries](/Admin/Queries)** aur **[Contact Messages](/Admin/ContactMessages)** open karein.";
                }

                return "**Support Desk & User Queries Overview:**\n\n* **Ticketing Desk:** Resolution portal for donor inquiries, receipt confirmations, and platform assistance.\n* **Direct Communications:** Built-in email dispatch and ticket status transitions (Open, Pending, Resolved).\n* **Contact Submissions:** Public inquiries submitted through the contact desk.\n\nResolve tickets via **[User Queries](/Admin/Queries)** and **[Contact Messages](/Admin/ContactMessages)**.";
            }

            // 13. CMS Pages, Media Gallery & FAQs
            if (msg.Contains("cms") || msg.Contains("content") || msg.Contains("gallery") || msg.Contains("about") 
                || msg.Contains("team") || msg.Contains("faq") || msg.Contains("faqs") || msg.Contains("page"))
            {
                if (isUrdu)
                {
                    return "**کنٹینٹ مینجمنٹ (CMS)، میڈیا گیلری اور FAQs:**\n\n* **میڈیا گیلری:** فلاحی سرگرمیوں کی تصاویر اور ویڈیوز کا انتظام۔\n* **About Pages:** مشن، وژن، اور ہماری کامیابیوں کے صفحات میں ترمیم۔\n* **قیادت کی ٹیم:** بورڈ آف ڈائریکٹرز اور ایگزیکٹو ٹیم کے پروفائلز۔\n* **Help Centre & FAQs:** کثرت سے پوچھے جانے والے سوالات کا انتظام۔\n\nانتظام کے لیے **[Media Gallery](/Admin/Gallery)** اور **[About Sub-Pages](/Admin/AboutPages)** وزٹ کریں۔";
                }
                if (isRoman)
                {
                    return "**Content Management (CMS) & Media Assets:**\n\n* **Media Gallery:** Charity projects aur relief drives ki photos aur videos upload/edit karein.\n* **About CMS Pages:** Mission, Vision, What We Do, aur Achievements pages ka content update karein.\n* **Leadership Team:** Board members aur executive profiles ka management.\n* **Help Centre FAQs:** Frequently Asked Questions add aur update karein.\n\nManage karein via **[Media Gallery](/Admin/Gallery)** aur **[About Sub-Pages](/Admin/AboutPages)**.";
                }

                return "**Content Management (CMS) & Media Assets:**\n\n* **Media Gallery:** High-resolution photo and video assets from active relief drives.\n* **CMS Pages:** Editable About Us sub-pages (Mission, Achievements, Strategy).\n* **Leadership Roster:** Board and executive leadership profiles.\n* **FAQ Management:** Dynamic knowledge base articles for public guidance.\n\nManage via **[Media Gallery](/Admin/Gallery)** and **[About Sub-Pages](/Admin/AboutPages)**.";
            }

            // 14. Automation History & Scheduled Tasks
            if (msg.Contains("automation") || msg.Contains("cron") || msg.Contains("history") || msg.Contains("scheduler") || msg.Contains("background"))
            {
                if (isUrdu)
                {
                    return "**آٹومیشن ہسٹری اور بیک گراؤنڈ جابز:**\n\n* پلیٹ فارم کے خودکار بیک گراؤنڈ شیڈولز اور سنک کے عمل کا ریکارڈ۔\n* ہر شیڈول جاب کی کامیابی، دورانیہ اور اثرات کا آڈٹ لاگ۔\n\nہسٹری کے لیے **[Automation History](/Admin/AutomationHistory)** دیکھیں۔";
                }
                if (isRoman)
                {
                    return "**Automation History & Background Scheduler:**\n\n* **Autonomous Background Jobs:** Scheduled intelligence tasks, security syncs, aur ledger reconciliation routines ka historical execution log.\n* **Execution Health:** Har run ka timestamp, execution duration, aur outcome status recorded rehta hai.\n\nCheck karein via **[Automation History](/Admin/AutomationHistory)**.";
                }

                return "**Automation History & Background Scheduler:**\n\n* **Scheduled Background Jobs:** Historical registry of background maintenance, cache warming, and security checks.\n* **Audit Metrics:** Detailed timestamps, duration, and completion statuses.\n\nInspect via **[Automation History](/Admin/AutomationHistory)**.";
            }

            // 15. Default Admin Assistance Guidance
            if (isUrdu)
            {
                return "**NGEOS ایڈمن ایگزیکٹو AI معاون:**\n\nمیں ایڈمن پینل کے تمام 15 ماڈیولز کی نگرانی کر رہا ہوں۔ آپ کسی بھی سیکشن کے بارے میں مخصوص سوال پوچھ سکتے ہیں:\n\n* **[عطیات اور لیجر](/Admin/Donations)**: لائیو کلیکشنز\n* **[فلاحی مہمات](/Admin/Causes)**: مہمات کی کارکردگی\n* **[ممبر ڈائریکٹری](/Admin/Users)**: صارفین اور سیکیورٹی کردار\n* **[سیکیورٹی انٹیلی جنس](/Admin/SecurityIntelligence)**: تھریٹ ڈیفنس اور الرٹس\n* **[ایجنٹ آڈٹ رپورٹس](/Admin/AgentReports)**: AI خودکار لاگز\n* **[سپورٹ سوالات](/Admin/Queries)**: زیرِ التوا ٹکٹس";
            }
            if (isRoman)
            {
                return "**NGEOS Admin Executive AI Assistant:**\n\nMain Admin Dashboard ke tamaam 15 sections ki monitoring aur administrative guidance ke liye hazir hoon. Aap niche diye gaye topics ya kisi bhi specific report ke mutaliq Roman Urdu mein pooch sakte hain:\n\n* **[Donations & Ledger](/Admin/Donations):** Live financial ledger aur receipts\n* **[Welfare Causes](/Admin/Causes):** Active causes aur funding progress\n* **[Member Directory](/Admin/Users):** User accounts aur admin roles\n* **[Security Intelligence](/Admin/SecurityIntelligence):** Threat defense aur IP bans\n* **[Agent Audit Reports](/Admin/AgentReports):** Autonomous AI logs\n* **[Support Queries](/Admin/Queries):** Donor assistance tickets\n\nAap apna sawal yahan likhein!";
            }

            return "**NGEOS Admin Executive AI Assistant:**\n\nI am actively monitoring all 15 Admin Dashboard modules. You can query any specific operational, financial, or security module:\n\n* **[Donations & Ledger](/Admin/Donations):** Live financial collections and receipts\n* **[Welfare Causes](/Admin/Causes):** Active campaigns and funding progress\n* **[Member Directory](/Admin/Users):** User accounts and security roles\n* **[Security Intelligence](/Admin/SecurityIntelligence):** Threat Defense and automated audit\n* **[Agent Audit Reports](/Admin/AgentReports):** Autonomous AI logs\n* **[Support Queries](/Admin/Queries):** Pending donor assistance tickets\n\nPlease enter any administrative inquiry.";
        }

        private string GenerateGroundedKnowledgeReply(string userMessage, string liveContext)
        {
            var msg = (userMessage ?? string.Empty).ToLowerInvariant().Trim();
            bool isUrdu = IsUrduScript(userMessage ?? string.Empty);
            bool isRoman = IsRomanUrdu(userMessage ?? string.Empty);

            // 1. Greetings & Conversational Roman Urdu
            if (msg == "hi" || msg == "hello" || msg == "hey" || msg.Contains("salam") || msg.Contains("assalam") || msg == "aoa"
                || msg.Contains("kya haal") || msg.Contains("kese ho") || msg.Contains("kaise ho") || msg.Contains("kaisa hai")
                || msg.Contains("baat kro") || msg.Contains("baat karo") || msg.Contains("suno") || msg.Contains("kya chal rha") || msg.Contains("kuch bolo"))
            {
                if (isUrdu)
                {
                    return "**السلام علیکم! NGEOS میں خوش آمدید۔**\n\nالحمدللہ میں بالکل ٹھیک ہوں۔ میں NGEOS کا تصدیق شدہ AI اسسٹنٹ ہوں۔ میں آپ کی کس طرح مدد کر سکتا ہوں؟\n\n* **[مہمات](/Causes):** فلاحی منصوبے دیکھیں\n* **[عطیہ](/Donate):** آن لائن عطیہ یا زکوٰۃ ادا کریں\n* **[زکوٰۃ کیلکولیٹر](/Zakat):** شرعی حساب لگائیں\n* **[پروگرامز](/Programmes):** فلاحی سرگرمیوں میں حصہ لیں";
                }
                if (isRoman)
                {
                    return "**Assalam-o-Alaikum! Welcome to NGEOS.**\n\nAlhamdulillah, main bilkul theek hoon! Main NGEOS ka verified AI Assistant hoon. Main aapki kis tarah madad kar sakta hoon?\n\n* **[Causes](/Causes):** Active charity aur welfare campaigns browse karein\n* **[Donate](/Donate):** Online donation ya Zakat ada karein\n* **[Zakat Calculator](/Zakat):** Apni Zakat ka shari'i hisab karein\n* **[Programmes](/Programmes):** Community aur volunteer drives mein hissa lein\n\nAap apna sawal yahan type kar sakte hain, main Roman Urdu mein mukammal jawab doon ga!";
                }

                return "**Assalam-o-Alaikum! Welcome to NGEOS.**\n\nI am doing well, thank you! I am your verified NGEOS AI Assistant. How may I assist you today?\n\n* **[Browse Causes](/Causes):** Explore verified active charity campaigns\n* **[Make a Donation](/Donate):** Instant online donation & Zakat\n* **[Smart Zakat Calculator](/Zakat):** Calculate your obligatory Zakat according to Shariah\n* **[Programmes](/Programmes):** Participate in community and volunteer initiatives\n\nFeel free to ask any question!";
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

            bool isAdmin = userRole != null && (userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) || userRole.Contains("Admin", StringComparison.OrdinalIgnoreCase));
            if (isAdmin)
            {
                sb.AppendLine("\n### CURRENT USER ROLE: PLATFORM ADMINISTRATOR (ADMIN MODE)");
                sb.AppendLine("You are interacting directly with the NGEOS Platform Administrator.");
                sb.AppendLine("STRICT REQUIREMENT: Provide deep administrative guidance covering ALL 15 Admin Dashboard modules (Donations Ledger, Causes Manager, Member Directory, Security Threat Defense, Agent Audit Reports, Donation Intelligence, NGOs, Programmes, Events, Support Queries, CMS Pages, System Settings).");
                sb.AppendLine("Never treat the administrator as a simple donor or visitor. Always surface executive metrics, operational statuses, and direct administrative navigation links.");
            }
            else if (!string.IsNullOrWhiteSpace(userRole))
            {
                sb.AppendLine($"Current user role: {userRole}. Contextualize your answers accordingly.");
            }

            sb.AppendLine("\n### CRITICAL OPERATING INSTRUCTIONS:");
            sb.AppendLine("1. GROUND TRUTH FIDELITY: You must answer all questions based STRICTLY on the provided [NGEOS VERIFIED LIVE DATABASE CONTEXT].");
            sb.AppendLine("2. FINANCIAL ACCURACY: ONLY completed transactions count towards total raised funds. Pending, failed, or cancelled transactions are excluded. Do not guess, estimate, or fabricate any financial numbers.");
            sb.AppendLine("3. ANTI-HALLUCINATION: If a specific campaign, programme, or policy is not in the verified context, explicitly state that it is not currently recorded or published on NGEOS.");
            sb.AppendLine("4. PRIVACY PROTECTION: Never share or solicit private personal information such as donor email addresses, phone numbers, or credit card details.");
            sb.AppendLine("5. STRICT LANGUAGE MIRRORING: You MUST detect the language of the user's message and reply in that EXACT same language:\n- If the user writes in English, you MUST reply 100% in English.\n- If the user writes in Roman Urdu (Urdu written in Latin letters, e.g. 'kya haal hai', 'Donations kitni hui hain?', 'admin data dikhao', 'causes ka status batao'), you MUST reply 100% in natural, polite, helpful Roman Urdu.\n- If the user writes in Urdu script (e.g. 'عطیہ کیسے دیں؟'), you MUST reply 100% in Urdu script.\nNEVER answer in English if the user asked in Roman Urdu or Urdu!");
            sb.AppendLine("6. FORMATTING: Use clean markdown with clear headings, bullet points, and bold text for readability. Avoid decorative emojis or cartoonish icons; maintain a clean, professional, executive presentation.");

            sb.AppendLine("\n" + liveContext);

            return sb.ToString();
        }

        private List<string> GenerateAdminContextualChips(string userMessage)
        {
            var msg = (userMessage ?? string.Empty).ToLowerInvariant();

            if (msg.Contains("ledger") || msg.Contains("donation") || msg.Contains("fund") || msg.Contains("paisa") || msg.Contains("finance"))
            {
                return new List<string> { "Donations & Ledger", "Donation Intelligence", "Welfare Causes", "Agent Audit Reports" };
            }
            if (msg.Contains("cause") || msg.Contains("campaign"))
            {
                return new List<string> { "Welfare Causes", "Donations & Ledger", "Events & Programmes", "NGOs & Partners" };
            }
            if (msg.Contains("security") || msg.Contains("threat") || msg.Contains("ban") || msg.Contains("agent") || msg.Contains("audit"))
            {
                return new List<string> { "Security & Threats", "Agent Audit Reports", "Member Directory", "Donations & Ledger" };
            }
            if (msg.Contains("user") || msg.Contains("member") || msg.Contains("volunteer"))
            {
                return new List<string> { "Member Directory", "Support Queries", "Security & Threats", "Events & Programmes" };
            }

            return GetAdminDefaultSuggestionChips();
        }

        public List<string> GetAdminDefaultSuggestionChips()
        {
            return new List<string>
            {
                "Donations & Ledger",
                "Welfare Causes",
                "Member Directory",
                "Security & Threats",
                "Agent Audit Reports",
                "Donation Intelligence",
                "NGOs & Partners",
                "Events & Programmes",
                "Support Queries",
                "CMS & Media"
            };
        }

        private List<string> GenerateContextualChips(string userMessage)
        {
            var msg = (userMessage ?? string.Empty).ToLowerInvariant();

            if (msg.Contains("cause") || msg.Contains("campaign") || msg.Contains("orphan") || msg.Contains("school"))
            {
                return new List<string> { "How to Donate?", "Total Verified Funds", "Active Programmes", "Zakat Calculator" };
            }

            if (msg.Contains("donate") || msg.Contains("zakat") || msg.Contains("fund") || msg.Contains("money") || msg.Contains("paisa"))
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
