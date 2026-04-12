using Portfolio.Application.Data.Models.SearchEngine.Index;

namespace Portfolio.Application.Services.Interfaces;

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