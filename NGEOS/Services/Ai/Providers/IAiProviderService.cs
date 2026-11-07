using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NGEOS.Services.Ai.Dtos;

namespace NGEOS.Services.Ai.Providers
{
    /// <summary>
    /// Contract for pluggable AI chat providers (Groq, Gemini, etc.).
    /// </summary>
    public interface IAiProviderService
    {
        string ProviderName { get; }
        bool IsConfigured { get; }
        Task<string?> GenerateReplyAsync(string systemPrompt, List<ChatMessageItem> history, string userMessage, CancellationToken cancellationToken = default);
    }
}
