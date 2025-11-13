using OpenAI.Embeddings;

namespace SiteBackend.Middleware.AIClient;

public partial class AiClient
{
    /// <summary>
    /// Generates an embedding for a given prompt using the specified embedding model.
    /// Meant for Search Queries AND Page Submissions its just converting string to vector maps and computing vectored
    /// distances using <see cref="Pgvector"/>'s Cosine, Hamming, Jaccard and L distances.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <returns>Float array of embedding values.</returns>
    public async Task<float[]> GetDenseEmbeddingAsync(string text)
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

    /// <summary>
    /// Generates a sparse embedding vector for a given text using the sparse embedding model.
    /// Sparse embeddings use high-dimensional, mostly-zero vectors that capture keyword presence.
    /// More interpretable than dense embeddings and useful for hybrid search strategies.
    /// </summary>
    /// <param name="text">The text to convert into a sparse vector embedding.</param>
    /// <returns>A float array representing the sparse embedding vector (30522 dimensions).</returns>
    /// <exception cref="ArgumentNullException">Thrown if text is null or whitespace.</exception>
    /// <exception cref="Exception">Thrown if embedding generation fails.</exception>
    public async Task<float[]> GetSparseEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentNullException(nameof(text));

        var response = await _sparseClient.GenerateEmbeddingAsync(text);
        if (response?.Value == null) throw new Exception("Failed to generate embedding.");

        return response.Value.ToFloats().ToArray();
    }
}