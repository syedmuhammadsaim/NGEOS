using System.Threading;
using System.Threading.Tasks;

namespace NGEOS.Services.Ai.Grounding
{
    /// <summary>
    /// Service responsible for querying live, verified NGEOS data from SQL Server via EF Core 10.
    /// Provides grounded, structured context blocks for AI consumption without exposing PII.
    /// </summary>
    public interface INgeosKnowledgeService
    {
        Task<string> GetGroundingContextAsync(string userMessage, CancellationToken cancellationToken = default);
    }
}
