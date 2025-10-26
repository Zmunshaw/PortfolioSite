namespace SiteBackend.Middleware.AIClient;

// TODO: for now this is local via docker but keep open for cloud workers or something else if it cheaper
// TODO: might want to switch to OpenAI API for ease of use with cloudflare workers if their AI Compute dont play well with OLlama
// It might make sense to split embedding to db and vectored queries - since embedding should happen locally(cheaper) but
// like 5 people are ever going to actually use the search 1 time so AI Compute workers for that makes it more reliable
// JUST SET LIMITS ON COMPUTE COSTS
public partial class AiClient
{
    /// <summary>
    /// Generates an embedding for a given prompt using the specified embedding model.
    /// Meant for Search Queries AND Page Submissions its just converting string to vector maps and computing vectored
    /// distances using <see cref="Pgvector"/>'s Cosine, Hamming, Jaccard and L distances.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="model">The model name, e.g., "nomic-embed-text".</param>
    /// <returns>Float array of embedding values.</returns>
    public async Task<float[]> GetDenseEmbeddingAsync(string text, string? model = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentNullException(nameof(text));

        var response = await _denseClient.GenerateEmbeddingAsync(text);

        if (response?.Value == null)
        {
            throw new Exception("Failed to generate embedding.");
        }

        return response.Value.ToFloats().ToArray();
    }

    public async Task<float[]> GetSparseEmbeddingAsync(string text, string? model = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentNullException(nameof(text));

        var response = await _sparseClient.GenerateEmbeddingAsync(text);

        if (response?.Value == null) throw new Exception("Failed to generate embedding.");

        return response.Value.ToFloats().ToArray();
    }
}