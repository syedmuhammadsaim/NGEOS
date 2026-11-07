using System;
using System.Collections.Generic;

namespace NGEOS.Services.Ai.Dtos
{
    /// <summary>
    /// Response model returned to the frontend chatbot UI.
    /// </summary>
    public class ChatbotResponseDto
    {
        public string Reply { get; set; } = string.Empty;
        public string ProviderUsed { get; set; } = "None"; // "Groq", "Gemini", "Fallback"
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<string> SuggestedActions { get; set; } = new();
    }
}
