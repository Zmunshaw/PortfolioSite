using Pgvector;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Services;

public interface IAIService
{
    Task<float[]> GetEmbeddingAsync(string title, string text);
    Task<List<TextEmbedding>> EmbedDocumentAsync(string title, string[][] wordChunks);
    Task<Vector> GetSearchVector(string query);
}