namespace NGEOS.Services.Ai
{
    /// <summary>
    /// Strongly typed configuration options for NGEOS AI Chatbot and multi-provider fallback.
    /// Loaded securely from .env and application environment variables.
    /// </summary>
    public class AiChatbotOptions
    {
        public const string SectionName = "AiChatbot";

        public string PrimaryProvider { get; set; } = "Groq";

        // Provider 1: Groq
        public string GroqApiKey { get; set; } = string.Empty;
        public string GroqModel { get; set; } = "llama-3.3-70b-versatile";

        // Provider 2: Gemini
        public string GeminiApiKey { get; set; } = string.Empty;
        public string GeminiModel { get; set; } = "gemini-2.0-flash";

        // Chatbot Controls
        public int MaxHistoryMessages { get; set; } = 8;
        public int TimeoutSeconds { get; set; } = 15;

        public bool IsGroqConfigured => !string.IsNullOrWhiteSpace(GroqApiKey) &&
                                        !GroqApiKey.Contains("your_groq_api_key");

        public bool IsGeminiConfigured => !string.IsNullOrWhiteSpace(GeminiApiKey) &&
                                          !GeminiApiKey.Contains("your_gemini_api_key");
    }
}
