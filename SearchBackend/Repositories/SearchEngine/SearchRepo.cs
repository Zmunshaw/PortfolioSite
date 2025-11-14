using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Polly;
using Polly.Registry;
using SearchBackend.Repositories.SearchEngine.Interfaces;
using SiteBackend.Database;
using SiteBackend.DTO.Website;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Services;

namespace SearchBackend.Repositories.SearchEngine;

/// <summary>
/// Repository for search operations with vector embeddings.
/// Supports hybrid search combining dense, sparse, and keyword matching.
/// Includes resilience patterns for transient failure handling.
/// </summary>
public class SearchRepo : ISearchRepo
{
    private readonly IAIService _aiService;
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    private readonly ILogger<SearchRepo> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    // MARKED: for #30
    private const int DefaultSimilarWordsLimit = 5;
    private const int DefaultSimilarEmbeddingsLimit = 25;
    private const float DefaultMaxDistance = float.MaxValue;

    public SearchRepo(IDbContextFactory<SearchEngineCtx> ctxFactory, ILogger<SearchRepo> logger, IAIService aiService,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _ctxFactory = ctxFactory;
        _logger = logger;
        _aiService = aiService;
        _resiliencePipeline = pipelineProvider.GetPipeline("db-backoff");
    }

    /// <summary>
    /// Executes a hybrid search combining dense embeddings, sparse embeddings, and keyword matching.
    /// Results are ranked using weighted combination of all three methods.
    /// </summary>
    /// <param name="request">Search request with query and vectorized data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results ranked by relevance score</returns>
    public async Task<IEnumerable<DTOSearchResult>> GetSearchResults(DTOSearchRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.DenseVector == null || request.SparseVector == null)
        {
            _logger.LogWarning("Search request missing vectorized data");
            return Enumerable.Empty<DTOSearchResult>();
        }

        try
        {
            var results = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);

                var matchingEmbeddings = await ctx.Pages
                    .AsNoTracking()
                    .Where(p => p.Content != null && p.Content.Embeddings.Any(e =>
                        (e.DenseEmbedding != null &&
                         e.DenseEmbedding.L2Distance(request.DenseVector) <= request.MaxDistance) ||
                        (e.SparseEmbedding != null &&
                         e.SparseEmbedding.CosineDistance(request.SparseVector) <= request.MaxDistance) ||
                        EF.Functions.Like(p.Content.Text, $"%{request.SearchQuery}%")))
                    .Include(pg => pg.Content)
                    .ThenInclude(c => c.Embeddings).ThenInclude(textEmbedding => textEmbedding.DenseEmbedding)
                    .Include(page => page.Content).ThenInclude(content => content.Embeddings)
                    .ThenInclude(textEmbedding => textEmbedding.SparseEmbedding)
                    .Include(pg => pg.Url)
                    .ToListAsync(ct);

                _logger.LogDebug("Found {PageCount} pages matching search criteria", matchingEmbeddings.Count);
                
                var scoredResults = matchingEmbeddings
                    .Select(page =>
                    {
                        var denseMin = page.Content.Embeddings
                            .Where(e => e.DenseEmbedding != null)
                            .Select(e => (double)e.DenseEmbedding.L2Distance(request.DenseVector))
                            .DefaultIfEmpty(double.MaxValue)
                            .Min();

                        var sparseMin = page.Content.Embeddings
                            .Where(e => e.SparseEmbedding != null)
                            .Select(e => (double)e.SparseEmbedding.CosineDistance(request.SparseVector))
                            .DefaultIfEmpty(double.MaxValue)
                            .Min();

                        var keywordMatch = EF.Functions.Like(page.Content.Text, $"%{request.SearchQuery}%");
                        
                        var score = (float)denseMin * request.DenseWeight +
                                   (float)sparseMin * request.SparseWeight +
                                   (keywordMatch ? 0 : request.KeywordWeight);

                        return new
                        {
                            Page = page,
                            Score = score,
                            DenseDistance = (float)denseMin,
                            SparseDistance = (float)sparseMin,
                            KeywordMatch = keywordMatch
                        };
                    })
                    .OrderBy(x => x.Score)
                    .ToList();
                
                var paginatedResults = scoredResults
                    .Skip((request.CurrentPage - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(x => new DTOSearchResult
                    {
                        ResultPage = x.Page,
                        ResultTitle = x.Page.Content.Title ?? "Untitled",
                        ResultDescription = x.Page.Content.Description ?? "",
                        ResultUrl = x.Page.Url.Location,
                        ResultScore = x.Score,
                        DenseDistance = x.DenseDistance,
                        SparseDistance = x.SparseDistance,
                        KeywordMatch = x.KeywordMatch
                    })
                    .ToList();

                _logger.LogInformation(
                    "Search query '{Query}' returned {ResultCount} results (page {Page})",
                    request.SearchQuery, paginatedResults.Count, request.CurrentPage);

                return paginatedResults;
            }, cancellationToken);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute search for query '{Query}' after retries",
                request.SearchQuery);
            throw;
        }
    }

    /// <summary>
    /// Finds similar words based on sparse vector distance.
    /// Useful for autocomplete, spell correction, and related term discovery.
    /// </summary>
    /// <param name="wordVector">Sparse embedding vector of the query word</param>
    /// <param name="take">Number of similar words to return</param>
    /// <param name="skip">Number of results to skip for pagination</param>
    /// <param name="maxDistance">Maximum cosine distance threshold (null = no limit)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of similar words ordered by distance (closest first)</returns>
    public async Task<IEnumerable<Word>> GetSimilarWords(SparseVector wordVector, int take = DefaultSimilarWordsLimit,
        int skip = 0, double? maxDistance = null, CancellationToken cancellationToken = default)
    {
        if (wordVector == null)
            throw new ArgumentNullException(nameof(wordVector));

        if (take <= 0)
        {
            _logger.LogWarning("GetSimilarWords called with invalid take value: {Take}", take);
            return Enumerable.Empty<Word>();
        }

        try
        {
            var words = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);

                var query = ctx.Words.AsNoTracking();
                
                if (maxDistance.HasValue)
                {
                    query = query.Where(w =>
                        w.SparseVector != null &&
                        w.SparseVector.CosineDistance(wordVector) <= maxDistance.Value);
                }
                
                var results = await query
                    .OrderBy(w => w.SparseVector != null
                        ? w.SparseVector.CosineDistance(wordVector)
                        : double.MaxValue)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(ct);

                _logger.LogDebug("Found {WordCount} similar words (take={Take}, skip={Skip})",
                    results.Count, take, skip);

                return results;
            }, cancellationToken);

            return words;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get similar words after retries");
            throw;
        }
    }

    /// <summary>
    /// Finds pages with similar sparse embeddings using cosine distance.
    /// Sparse embeddings are efficient for keyword-based retrieval.
    /// </summary>
    /// <param name="queryVector">Sparse embedding vector to match</param>
    /// <param name="limit">Maximum number of pages to return</param>
    /// <param name="skip">Number of results to skip for pagination</param>
    /// <param name="maxDistance">Maximum cosine distance threshold (null = no limit)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pages ordered by embedding similarity</returns>
    public async Task<IEnumerable<Page>> GetSimilarSparseEmbeddingsAsync(SparseVector queryVector, 
        int limit = DefaultSimilarEmbeddingsLimit, int skip = 0, double? maxDistance = null,
        CancellationToken cancellationToken = default)
    {
        if (queryVector == null)
            throw new ArgumentNullException(nameof(queryVector));

        if (limit <= 0)
        {
            _logger.LogWarning("GetSimilarSparseEmbeddingsAsync called with invalid limit: {Limit}", limit);
            return Enumerable.Empty<Page>();
        }

        try
        {
            var pages = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                
                var embeddingIds = await ctx.TextEmbeddings
                    .AsNoTracking()
                    .Where(te => te.SparseEmbedding != null)
                    .Where(te => !maxDistance.HasValue ||
                                 te.SparseEmbedding.CosineDistance(queryVector) <= maxDistance.Value)
                    .OrderBy(te => te.SparseEmbedding.CosineDistance(queryVector))
                    .Select(te => te.TextEmbeddingID)
                    .Skip(skip)
                    .Take(limit)
                    .ToListAsync(ct);

                if (!embeddingIds.Any())
                {
                    _logger.LogDebug("No similar sparse embeddings found");
                    return Enumerable.Empty<Page>();
                }
                
                var matchingPages = await ctx.Pages
                    .AsNoTracking()
                    .Where(pg => pg.Content.Embeddings.Any(emb => embeddingIds.Contains(emb.TextEmbeddingID)))
                    .Include(pg => pg.Content)
                    .ThenInclude(c => c.Embeddings)
                    .Include(pg => pg.Url)
                    .ToListAsync(ct);

                _logger.LogDebug("Found {PageCount} pages with similar sparse embeddings", matchingPages.Count);
                return matchingPages;
            }, cancellationToken);

            return pages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get similar sparse embeddings after retries");
            throw;
        }
    }

    /// <summary>
    /// Finds pages with similar dense embeddings using L2 distance.
    /// Dense embeddings capture semantic meaning and are efficient for semantic search.
    /// </summary>
    /// <param name="queryVector">Dense embedding vector (768 dimensions) to match</param>
    /// <param name="limit">Maximum number of pages to return</param>
    /// <param name="skip">Number of results to skip for pagination</param>
    /// <param name="maxDistance">Maximum L2 distance threshold (null = no limit)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pages ordered by semantic similarity</returns>
    public async Task<IEnumerable<Page>> GetSimilarDenseEmbeddingsAsync(Vector queryVector, 
        int limit = DefaultSimilarEmbeddingsLimit, int skip = 0, double? maxDistance = null,
        CancellationToken cancellationToken = default)
    {
        if (queryVector == null)
            throw new ArgumentNullException(nameof(queryVector));

        if (limit <= 0)
        {
            _logger.LogWarning("GetSimilarDenseEmbeddingsAsync called with invalid limit: {Limit}", limit);
            return Enumerable.Empty<Page>();
        }

        try
        {
            var pages = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                
                var embeddingIds = await ctx.TextEmbeddings
                    .AsNoTracking()
                    .Where(te => te.DenseEmbedding != null)
                    .Where(te => !maxDistance.HasValue ||
                                 te.DenseEmbedding.L2Distance(queryVector) <= maxDistance.Value)
                    .OrderBy(te => te.DenseEmbedding.L2Distance(queryVector))
                    .Select(te => te.TextEmbeddingID)
                    .Skip(skip)
                    .Take(limit)
                    .ToListAsync(ct);

                if (!embeddingIds.Any())
                {
                    _logger.LogDebug("No similar dense embeddings found");
                    return Enumerable.Empty<Page>();
                }
                
                var matchingPages = await ctx.Pages
                    .AsNoTracking()
                    .Where(pg => pg.Content.Embeddings.Any(emb => embeddingIds.Contains(emb.TextEmbeddingID)))
                    .Include(pg => pg.Content)
                    .ThenInclude(c => c.Embeddings)
                    .Include(pg => pg.Url)
                    .ToListAsync(ct);

                _logger.LogDebug("Found {PageCount} pages with similar dense embeddings", matchingPages.Count);
                return matchingPages;
            }, cancellationToken);

            return pages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get similar dense embeddings after retries");
            throw;
        }
    }

    /// <summary>
    /// Executes a hybrid similarity search combining dense and sparse embeddings.
    /// Finds pages semantically and lexically similar to the search request.
    /// </summary>
    /// <param name="searchRequest">Search request with query and vectorized data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pages ranked by combined similarity score</returns>
    public async Task<IEnumerable<Page>> GetSimilarEmbeddingsAsync(DTOSearchRequest searchRequest,
        CancellationToken cancellationToken = default)
    {
        if (searchRequest == null)
            throw new ArgumentNullException(nameof(searchRequest));

        if (searchRequest.DenseVector == null || searchRequest.SparseVector == null)
        {
            _logger.LogWarning("Search request missing vectorized data for similarity search");
            return Enumerable.Empty<Page>();
        }

        try
        {
            var pages = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                
                var embeddingsWithScores = await ctx.TextEmbeddings
                    .AsNoTracking()
                    .Where(te => te.DenseEmbedding != null || te.SparseEmbedding != null)
                    .Select(te => new
                    {
                        te.TextEmbeddingID,
                        te.Content,
                        DenseDistance = te.DenseEmbedding != null
                            ? te.DenseEmbedding.L2Distance(searchRequest.DenseVector)
                            : (double?)null,
                        SparseDistance = te.SparseEmbedding != null
                            ? te.SparseEmbedding.CosineDistance(searchRequest.SparseVector)
                            : (double?)null
                    })
                    .ToListAsync(ct);
                
                var scoredEmbeddings = embeddingsWithScores
                    .Select(e => new
                    {
                        e.TextEmbeddingID,
                        e.Content,
                        Score = (float)(
                            (e.DenseDistance ?? DefaultMaxDistance) * searchRequest.DenseWeight +
                            (e.SparseDistance ?? DefaultMaxDistance) * searchRequest.SparseWeight)
                    })
                    .OrderBy(x => x.Score)
                    .Take(searchRequest.PageSize)
                    .Select(x => x.TextEmbeddingID)
                    .ToList();

                if (!scoredEmbeddings.Any())
                {
                    _logger.LogDebug("No similar embeddings found for hybrid search");
                    return Enumerable.Empty<Page>();
                }
                
                var matchingPages = await ctx.Pages
                    .AsNoTracking()
                    .Where(p => p.Content.Embeddings.Any(e => scoredEmbeddings.Contains(e.TextEmbeddingID)))
                    .Include(p => p.Content)
                    .ThenInclude(c => c.Embeddings)
                    .Include(p => p.Url)
                    .ToListAsync(ct);

                _logger.LogInformation(
                    "Hybrid similarity search returned {PageCount} pages",
                    matchingPages.Count);

                return matchingPages;
            }, cancellationToken);

            return pages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get similar embeddings after retries");
            throw;
        }
    }
}