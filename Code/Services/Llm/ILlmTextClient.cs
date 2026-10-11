using System.Threading;
using System.Threading.Tasks;

namespace RPGGame.Services.Llm
{
    /// <summary>Minimal text-completion client used by optional AI features (victory limerick, etc.).</summary>
    public interface ILlmTextClient
    {
        Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
    }
}
