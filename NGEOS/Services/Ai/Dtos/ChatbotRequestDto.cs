using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NGEOS.Services.Ai.Dtos
{
    /// <summary>
    /// User incoming message payload sent to /api/chatbot/message.
    /// </summary>
    public class ChatbotRequestDto
    {
        [Required(ErrorMessage = "Message is required.")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 1000 characters.")]
        public string Message { get; set; } = string.Empty;

        public List<ChatMessageItem> ConversationHistory { get; set; } = new();

        public string? SessionId { get; set; }
    }
}
