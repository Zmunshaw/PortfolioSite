namespace SiteBackend.Services;

public interface IAIService
{
    Task<float[]> GetEmbeddingAsync(string text);
}