namespace SiteBackend.Middleware.AIClient;

public interface IAiClient
{
    Task<float[]> GetDenseEmbeddingAsync(string text);
    Task<float[]> GetSparseEmbeddingAsync(string text);
}