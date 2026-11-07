namespace NGEOS.Services.Ai.Dtos
{
    /// <summary>
    /// Represents a single conversation turn in chat history.
    /// </summary>
    public class ChatMessageItem
    {
        public string Role { get; set; } = "user"; // "user", "assistant", "system"
        public string Content { get; set; } = string.Empty;
    }
}
