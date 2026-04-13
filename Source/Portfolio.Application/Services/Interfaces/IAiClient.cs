namespace Portfolio.Application.Services.Interfaces;

public interface IAiClient
{
    Task<float[]> GetDenseEmbeddingAsync(string text, string? model = null);
    Task<float[]> GetSparseEmbeddingAsync(string text, string? model = null);
}
