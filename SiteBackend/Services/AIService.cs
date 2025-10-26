using Pgvector;
using SiteBackend.Middleware.AIClient;
using SiteBackend.Models.SearchEngine;
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

    #region Search

    public async Task<List<Word>> EmbedWordsAsync(List<Word> wordsToEmbed)
    {
        _logger.LogDebug($"Embeding {wordsToEmbed.Count} words...");
        foreach (var word in wordsToEmbed)
            word.Embedding = await GetSparseVectorsAsync(GetSparseEmbeddingPrompt(word.Text));

        return wordsToEmbed.ToList();
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
    public async Task<List<TextEmbedding>> EmbedDocumentAsync(string[][] wordChunks)
    {
        _logger.LogDebug($"Embedding chunk count: {wordChunks.Length}");
        List<TextEmbedding> results = new();
        // TODO: add ctoke
        foreach (var chunk in wordChunks)
        {
            var wordChunk = string.Join(" ", chunk);
            var embeddingVector = await GetDenseVectorsAsync(GetDenseEmbeddingPrompt(wordChunk));
            var emb = new TextEmbedding(wordChunk, embeddingVector);
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
    public async Task<Vector> GetSearchVectorAsync(string query)
    {
        return await GetDenseVectorsAsync(GetDenseSearchPrompt(query));
    }

    public async Task<List<SparseVector>> GetKeywordVectorsAsync(string query)
    {
        _logger.LogDebug($"Getting keyword vectors for {query}");
        var vectors = new List<SparseVector>();
        var words = query.Split(' ');
        List<Task<SparseVector>> embedTasks =
        [
            GetSparseVectorsAsync(GetSparseSearchPrompt(query))
        ];
        foreach (var word in words)
            embedTasks.Add(GetSparseVectorsAsync(GetSparseSearchPrompt(query)));

        Task.WaitAll(embedTasks);

        return embedTasks.Select(tk => tk.Result).ToList();
    }

    #endregion

    #region General Purpose

    public async Task<Vector> GetDenseVectorsAsync(string query)
    {
        return new Vector(await _aiClient.GetDenseEmbeddingAsync(query));
    }

    public async Task<SparseVector> GetSparseVectorsAsync(string query)
    {
        return new SparseVector(await _aiClient.GetSparseEmbeddingAsync(query));
    }

    #endregion

    #region Prompts

    // TODO: figure out if granite-embedding wants a prompt
    private string GetDenseEmbeddingPrompt(string text)
    {
        var prompt = $"{text}";
        return prompt;
    }

    // TODO: figure out if granite-embedding wants a prompt
    private string GetDenseSearchPrompt(string query)
    {
        return $"{query}";
    }

    private string GetSparseEmbeddingPrompt(string query)
    {
        var prompt = $"{query}";
        return prompt;
    }

    private string GetSparseSearchPrompt(string query)
    {
        var prompt = $"{query}";
        return prompt;
    }

    #endregion
}