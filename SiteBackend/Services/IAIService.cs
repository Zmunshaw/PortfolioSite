using Pgvector;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Services;

public interface IAIService
{
    #region General Purpose

    Task<Vector> GetDenseVectorsAsync(string query);
    Task<SparseVector> GetSparseVectorsAsync(string query);

    #endregion

    #region Search

    Task<Vector> GetSearchVectorAsync(string text);
    Task<List<TextEmbedding>> EmbedDocumentAsync(string[][] wordChunks);

    Task<List<SparseVector>> GetKeywordVectorsAsync(string text);
    Task<List<Word>> EmbedWordsAsync(List<Word> wordsToEmbed);

    #endregion
}