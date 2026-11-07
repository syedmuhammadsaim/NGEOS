using System.Threading;
using System.Threading.Tasks;
using NGEOS.Services.Ai.Dtos;

namespace NGEOS.Services.Ai.Orchestration
{
    /// <summary>
    /// Coordinates database grounding, AI provider routing, and automatic fallback.
    /// </summary>
    public interface IAiChatbotService
    {
        Task<ChatbotResponseDto> ProcessMessageAsync(ChatbotRequestDto request, string? userRole = null, CancellationToken cancellationToken = default);
    }
}
