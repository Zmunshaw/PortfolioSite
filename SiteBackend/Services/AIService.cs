using Pgvector;
using SiteBackend.Middleware.AIClient;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Services;

public class AIService : IAIService
{
    private readonly IAiClient _aiClient;
    private readonly ILogger<AIService> _logger;

    public AIService(ILogger<AIService> logger, IAiClient aiClient)
    {
        _logger = logger;
        _aiClient = aiClient;
    }

    /// <summary>
    /// Generates an embedding for a given prompt using the specified embedding model.
    /// Meant for Page Submissions its just converting string to vector maps and computing vectored
    /// distances using <see cref="Pgvector"/>'s Cosine, Hamming, Jaccard and L distances.
    /// </summary>
    /// <param name="title">Document Title.</param>
    /// <param name="text">document text.</param>
    /// <returns>Float array of embedding values.</returns>
    public async Task<float[]> GetEmbeddingAsync(string title, string text)
    {
        var result = await _aiClient.GetDenseEmbeddingAsync(GetPromptEmbedding(title, text));
        _logger.LogDebug($"Embedding result count: {result.Length}");
        return result;
    }

    public async Task<List<TextEmbedding>> EmbedDocumentAsync(string title, string[][] wordChunks)
    {
        _logger.LogDebug($"Embedding chunk count: {wordChunks.Length}");
        List<TextEmbedding> results = new();
        // TODO: add ctoke
        foreach (var chunk in wordChunks)
        {
            var wordChunk = string.Join(" ", chunk);
            var embeddingVector = await GetEmbeddingAsync(title, wordChunk);
            var emb = new TextEmbedding(wordChunk, new Vector(embeddingVector));
            results.Add(emb);
        }

        return results;
    }

    public async Task<Vector> GetSearchVector(string query)
    {
        return new Vector(await _aiClient.GetDenseEmbeddingAsync(GetPrompt("search result", query)));
    }

    string GetPromptEmbedding(string title, string text)
    {
        string prompt = GetPrompt($"{{title |{title}}}", $"text: {text}");
        return prompt;
    }

    string GetPrompt(string task, string query)
    {
        return $"task: {task} | query: {query}";
    }
}