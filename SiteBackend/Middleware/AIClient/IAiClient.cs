namespace SiteBackend.Middleware.AIClient;

public interface IAiClient
{
    #region Models
    Task PullModel(string model);
    Task<bool> SetModel(int model);
    Task<bool> SetModel(string model);
    #endregion
    
    #region Embeddings
    Task<float[]> GetEmbeddingAsync (string text, string? model = null);
    #endregion
    
    #region Completions
    
    #endregion
}