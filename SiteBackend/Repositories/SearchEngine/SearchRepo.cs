using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SiteBackend.Database;
using SiteBackend.DTO.Website;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine.Interfaces;
using SiteBackend.Services;

namespace SiteBackend.Repositories.SearchEngine;

public class SearchRepo : ISearchRepo
{
    private readonly IAIService _aiService;
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    private readonly ILogger<SearchRepo> _logger;

    public SearchRepo(IDbContextFactory<SearchEngineCtx> ctxFactory, ILogger<SearchRepo> logger, IAIService aiService)
    {
        _ctxFactory = ctxFactory;
        _logger = logger;
        _aiService = aiService;
    }

    // TODO: EF.Functions.Like Says it can possibly throw, figure that out.
    // TODO: This isnt the greatest algo, maybe normalize/average the results per chunk of context on vectors or smthn
    // TODO: DefaultIfEmpty(maxVal) is fine for now but it invalidates keywording fallback during scoring(i think).
    public async Task<IEnumerable<DTOSearchResult>> GetSearchResults(DTOSearchRequest request)
    {
        await using var ctx = await _ctxFactory.CreateDbContextAsync();

        // OKAY, this is a lot but the first SELECT gets semantic context for page, the approximate keywording and exact keywords
        // The first WHERE checks if it fits through one of the filters (is it close enough in meaning or keywording)
        // the second SELECT assigns weighted scores to the results
        // FINALLY we sort the scores and then break any ties with whoever means more to the query from a context point.
        var searchQuery = ctx.Pages.AsQueryable()
            .Select(page => new
            {
                Page = page,
                DenseDistance = page.Content.Embeddings
                    .Where(ct => ct.DenseEmbedding != null)
                    .Select(ct => ct.DenseEmbedding.L2Distance(request.DenseVector))
                    .DefaultIfEmpty(float.MaxValue) // Skip if no embeddings
                    .Min(),

                SparseDistance = page.Content.Embeddings
                    .Where(ct => ct.SparseEmbedding != null)
                    .Select(ct => ct.SparseEmbedding.CosineDistance(request.SparseVector))
                    .DefaultIfEmpty(float.MaxValue) // Skip if no embeddings
                    .Min(),

                KeywordMatch = EF.Functions.Like(page.Content.Text, $"%{request.SearchQuery}%")
            })
            .Where(x =>
                x.DenseDistance <= request.MaxDistance || x.SparseDistance <= request.MaxDistance || x.KeywordMatch)
            .Select(x => new
            {
                x.Page,
                ResultScore = x.DenseDistance * request.DenseWeight +
                              x.SparseDistance * request.SparseWeight +
                              (x.KeywordMatch ? 0.0 : request.KeywordWeight),
                x.DenseDistance, x.SparseDistance, x.KeywordMatch
            })
            .OrderBy(x => x.ResultScore)
            .ThenBy(x => x.DenseDistance);

        var total = await searchQuery.CountAsync();
        var results = await searchQuery
            .Skip((request.CurrentPage - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new DTOSearchResult(x.Page, x.ResultScore, x.DenseDistance, x.SparseDistance, x.KeywordMatch))
            .ToListAsync();

        return results;
    }

    public async Task<IEnumerable<Word>> GetSimilarWords(SparseVector wordVector, int take = 5, int skip = 0,
        double? maxDistance = null)
    {
        var results = new List<Word>();

        await using var ctx = await _ctxFactory.CreateDbContextAsync();

        var baseQuery = ctx.Words.AsQueryable();

        if (maxDistance.HasValue)
            baseQuery = baseQuery.Where(w => w.SparseVector.CosineDistance(wordVector) <= maxDistance);

        var orderedQuery = baseQuery
            .OrderBy(w => w.SparseVector.CosineDistance(wordVector));

        results.AddRange(await orderedQuery.Skip(skip).Take(take).ToListAsync());
        return results;
    }

    public async Task<IEnumerable<Page>> GetSimilarSparseEmbeddingsAsync(
        SparseVector queryVector,
        int limit = 25, int skip = 0,
        double? maxDistance = null)
    {
        await using var ctx = await _ctxFactory.CreateDbContextAsync();

        var baseQuery = ctx.TextEmbeddings.AsQueryable();

        if (maxDistance.HasValue)
            baseQuery = baseQuery.Where(te =>
                te.SparseEmbedding != null && te.SparseEmbedding.L2Distance(queryVector) <= maxDistance.Value);

        var orderedQuery = baseQuery
            .OrderBy(te => te.SparseEmbedding != null ? te.SparseEmbedding.L2Distance(queryVector) : (double?)null)
            .Skip(skip)
            .Take(limit);

        var embeddingResults = await orderedQuery.ToListAsync();
        var ids = embeddingResults.Select(e => e.TextEmbeddingID).ToList();

        var pages = await ctx.Pages
            .Where(pg => pg.Content.Embeddings.Any(emb => ids.Contains(emb.TextEmbeddingID)))
            .Include(pg => pg.Content)
            .ThenInclude(ct => ct.Embeddings)
            .Include(pg => pg.Url)
            .ToListAsync();

        return pages;
    }

    public async Task<IEnumerable<Page>> GetSimilarDenseEmbeddingsAsync(
        Vector queryVector,
        int limit = 25, int skip = 0,
        double? maxDistance = null)
    {
        await using var ctx = await _ctxFactory.CreateDbContextAsync();

        var baseQuery = ctx.TextEmbeddings.AsQueryable();

        if (maxDistance.HasValue)
            baseQuery = baseQuery.Where(te => te.DenseEmbedding != null && te.DenseEmbedding
                .CosineDistance(queryVector) <= maxDistance);

        var orderedQuery = baseQuery
            .OrderBy(te => te.DenseEmbedding != null ? te.DenseEmbedding.L2Distance(queryVector) : (double?)null)
            .Skip(skip)
            .Take(limit);

        var embeddingResults = await orderedQuery.ToListAsync();
        var ids = embeddingResults.Select(e => e.TextEmbeddingID).ToList();

        var pages = await ctx.Pages
            .Where(pg => pg.Content.Embeddings.Any(emb => ids.Contains(emb.TextEmbeddingID)))
            .Include(pg => pg.Content)
            .ThenInclude(ct => ct.Embeddings)
            .Include(pg => pg.Url)
            .ToListAsync();

        return pages;
    }

    public async Task<IEnumerable<Page>> GetSimilarEmbeddingsAsync(DTOSearchRequest searchRequest)
    {
        var pages = new List<Page>();


        return pages;
    }
}