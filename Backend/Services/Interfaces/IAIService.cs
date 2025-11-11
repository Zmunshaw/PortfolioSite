using Pgvector;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Services;

public interface IAIService
{
    #region General Purpose

    Task<Vector> GetDenseVectorsAsync(string query);
    Task<SparseVector> GetSparseVectorsAsync(string query);

    #endregion

    #region Search

    Task<Vector> GetDenseSearchVectorAsync(string text);
    Task<SparseVector> GetSparseSearchVectorAsync(string text);
    Task<List<TextEmbedding>> EmbedDocumentAsync(string[][] wordChunks);

    #endregion
}