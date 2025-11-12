namespace SiteBackend.Middleware.AIClient;

public interface IAiClient
{
    #region Embeddings

    Task<float[]> GetDenseEmbeddingAsync(string text, string? model = null);
    Task<float[]> GetSparseEmbeddingAsync(string text, string? model = null);

    #endregion

    #region Completions

    #endregion
}