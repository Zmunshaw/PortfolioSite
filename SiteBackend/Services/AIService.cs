using System.Collections.Concurrent;
using Pgvector;
using SiteBackend.Configs;
using SiteBackend.Middleware.AIClient;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Services;

public class AIService : IAIService
{
    private readonly ILogger<AIService> _logger;
    private readonly IAiClient  _aiClient;

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
        var result = await _aiClient.GetEmbeddingAsync(GetPromptEmbedding(title, text));
        _logger.LogDebug($"Embedding result count: {result.Length}");
        return result;
    }

    public async Task<List<TextEmbedding>> EmbedDocumentAsync(string title, string[][] wordChunks)
    {
        _logger.LogDebug($"Embedding chunk count: {wordChunks.Length}");
        ConcurrentBag<TextEmbedding> results = new();
        // TODO: add ctoken
        await Parallel.ForEachAsync(wordChunks, new ParallelOptions { MaxDegreeOfParallelism = 4 }, 
            async (chunk, token) =>
        {
            var wordChunk = string.Join(" ", chunk);
            var embeddingVector = await GetEmbeddingAsync(title, wordChunk);
            var emb = new TextEmbedding(wordChunk, new Vector(embeddingVector));
            results.Add(emb);
        });
        
        return results.ToList();
    }
    
    public Task<List<Page>> GetSearchResults(string query)
    {
        throw new NotImplementedException();
    }

    string GetPromptEmbedding(string title, string text)
    {
        string prompt = GetPrompt($"{{title |{title}}}", $"text: {text}");
        _logger.LogDebug("Prompt generated: {prompt}", prompt);
        return prompt;
    }
    
    string GetPrompt(string task, string query)
    {
        return $"task: {task} | query: {query}";
    }
}