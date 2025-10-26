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
    /// Generates an embedding for a given prompt using the dense embedding model.
    /// Meant for Page Submissions its just converting string to vector maps and computing vectored
    /// distances using <see cref="Pgvector"/>'s Cosine, Hamming, Jaccard and L distances.
    /// </summary>
    /// <param name="title">Document Title.</param>
    /// <param name="text">document text.</param>
    /// <returns>Float array of embedding values.</returns>
    public async Task<float[]> GetEmbeddingAsync(string title, string text)
    {
        var result = await _aiClient.GetDenseEmbeddingAsync(GetDenseEmbedding(text));
        _logger.LogDebug($"Embedding result count: {result.Length}");
        return result;
    }

    /// <summary>
    ///     Use for generating vectors on a document for
    ///     <see href="https://cohere.com/llmu/what-is-semantic-search">Semantic Search</see>
    ///     ?this takes sentences/paragraphics and (normally)plots them to a hypersphere.?
    ///     it uses <see href="https://milvus.io/docs/dense-vector.md">Dense Vectors</see> which increases storage costs
    ///     but enables larger "semantic scope"
    /// </summary>
    /// <param name="wordChunks">[[Take, a, document], [break, it, into, paragraphs, and, split, the, words]]</param>
    /// <returns></returns>
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

    /// <summary>
    ///     Use to get query vectors for a
    ///     <see href="https://cohere.com/llmu/what-is-semantic-search">Semantic Search</see>
    /// </summary>
    /// <param name="query">Why did I choose a AI-powered search engine for a first project in a portfolio site?</param>
    /// <returns>
    ///     768 floating point vectors that represent the semantic meaning behind that query in the
    ///     form of a pgVector type, it can be unpacked to <see cref="float">float</see>[]
    /// </returns>
    public async Task<Vector> GetSearchVector(string query)
    {
        return new Vector(await _aiClient.GetDenseEmbeddingAsync(GetDenseSearchPrompt(query)));
    }

    // TODO: figure out if granite-embedding wants a prompt
    private string GetDenseEmbedding(string text)
    {
        var prompt = $"{text}";
        return prompt;
    }

    // TODO: figure out if granite-embedding wants a prompt
    private string GetDenseSearchPrompt(string query)
    {
        return $"{query}";
    }
}