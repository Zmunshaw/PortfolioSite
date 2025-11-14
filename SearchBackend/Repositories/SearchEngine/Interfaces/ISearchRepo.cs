using Pgvector;
using SiteBackend.DTO.Website;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;

namespace SearchBackend.Repositories.SearchEngine.Interfaces;

/// <summary>
/// Repository interface for search operations with vector embeddings.
/// Supports hybrid search combining dense, sparse, and keyword matching.
/// </summary>
public interface ISearchRepo
{
    /// <summary>
    /// Executes a hybrid search combining dense embeddings, sparse embeddings, and keyword matching.
    /// Results are ranked using weighted combination of all three methods.
    /// </summary>
    /// <param name="request">Search request with query and vectorized data</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Paginated search results ranked by relevance score</returns>
    Task<IEnumerable<DTOSearchResult>> GetSearchResults(
        DTOSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds similar words based on sparse vector distance.
    /// Useful for autocomplete, spell correction, and related term discovery.
    /// </summary>
    /// <param name="wordVector">Sparse embedding vector of the query word</param>
    /// <param name="take">Number of similar words to return (default: 5)</param>
    /// <param name="skip">Number of results to skip for pagination (default: 0)</param>
    /// <param name="maxDistance">Maximum cosine distance threshold (null = no limit)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>List of similar words ordered by distance (closest first)</returns>
    Task<IEnumerable<Word>> GetSimilarWords(
        SparseVector wordVector,
        int take = 5,
        int skip = 0,
        double? maxDistance = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds pages with similar sparse embeddings using cosine distance.
    /// Sparse embeddings are efficient for keyword-based retrieval.
    /// </summary>
    /// <param name="queryVector">Sparse embedding vector to match</param>
    /// <param name="limit">Maximum number of pages to return (default: 25)</param>
    /// <param name="skip">Number of results to skip for pagination (default: 0)</param>
    /// <param name="maxDistance">Maximum cosine distance threshold (null = no limit)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Pages ordered by embedding similarity</returns>
    Task<IEnumerable<Page>> GetSimilarSparseEmbeddingsAsync(
        SparseVector queryVector,
        int limit = 25,
        int skip = 0,
        double? maxDistance = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds pages with similar dense embeddings using L2 distance.
    /// Dense embeddings capture semantic meaning and are efficient for semantic search.
    /// </summary>
    /// <param name="queryVector">Dense embedding vector (768 dimensions) to match</param>
    /// <param name="limit">Maximum number of pages to return (default: 25)</param>
    /// <param name="skip">Number of results to skip for pagination (default: 0)</param>
    /// <param name="maxDistance">Maximum L2 distance threshold (null = no limit)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Pages ordered by semantic similarity</returns>
    Task<IEnumerable<Page>> GetSimilarDenseEmbeddingsAsync(
        Vector queryVector,
        int limit = 25,
        int skip = 0,
        double? maxDistance = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a hybrid similarity search combining dense and sparse embeddings.
    /// Finds pages semantically and lexically similar to the search request.
    /// </summary>
    /// <param name="searchRequest">Search request with query and vectorized data</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Pages ranked by combined similarity score</returns>
    Task<IEnumerable<Page>> GetSimilarEmbeddingsAsync(
        DTOSearchRequest searchRequest,
        CancellationToken cancellationToken = default);
}