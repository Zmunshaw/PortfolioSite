using SiteBackend.Configs;
using SiteBackend.Middleware.AIClient;

namespace SiteBackend.Services;

public class AIService : IAIService
{
    private readonly ILogger<AIService> _logger;
    private readonly EmbeddingClient  _embeddingClient;

    public AIService(ILogger<AIService> logger)
    {
        _logger = logger;
        _embeddingClient = new();
    }
    
    /// <summary>
    /// Generates an embedding for a given prompt using the specified embedding model.
    /// Meant for Search Queries AND Page Submissions its just converting string to vector maps and computing vectored
    /// distances using <see cref="Pgvector"/>'s Cosine, Hamming, Jaccard and L distances.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="model">The model name, e.g., "nomic-embed-text".</param>
    /// <returns>Float array of embedding values.</returns>
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var result = await _embeddingClient.GetEmbeddingAsync(text);
        return result;
    }
}