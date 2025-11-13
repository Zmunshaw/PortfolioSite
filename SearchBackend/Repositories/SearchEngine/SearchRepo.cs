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

    public async Task<IEnumerable<DTOSearchResult>> GetSearchResults(DTOSearchRequest request)
    {
        await using var ctx = await _ctxFactory.CreateDbContextAsync();

        var results = ctx.Pages
            .Where(p => p.Content.Embeddings.Any(e =>
                (e.DenseEmbedding != null && e.DenseEmbedding.L2Distance(request.DenseVector) <= request.MaxDistance) ||
                (e.SparseEmbedding != null &&
                 e.SparseEmbedding.CosineDistance(request.SparseVector) <= request.MaxDistance) ||
                EF.Functions.Like(p.Content.Text, $"%{request.SearchQuery}%")))
            .Include(pg => pg.Content)
            .Include(pg => pg.Url)
            .Select(page => new
            {
                Page = page,
                DenseMin = (float?)page.Content.Embeddings
                    .Where(e => e.DenseEmbedding != null)
                    .Select(e => e.DenseEmbedding.L2Distance(request.DenseVector))
                    .Min() ?? float.MaxValue,
                SparseMin = (float?)page.Content.Embeddings
                    .Where(e => e.SparseEmbedding != null)
                    .Select(e => e.SparseEmbedding.CosineDistance(request.SparseVector))
                    .Min() ?? float.MaxValue,
                KeywordMatch = EF.Functions.Like(page.Content.Text, $"%{request.SearchQuery}%")
            })
            .AsEnumerable()
            .OrderBy(x =>
                x.DenseMin * request.DenseWeight + x.SparseMin * request.SparseWeight +
                (x.KeywordMatch ? 0 : request.KeywordWeight))
            .Skip((request.CurrentPage - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new DTOSearchResult
            {
                ResultPage = x.Page,
                ResultTitle = x.Page.Content.Title,
                ResultDescription = x.Page.Content.Description,
                ResultUrl = x.Page.Url.Location,
                ResultScore = x.DenseMin * request.DenseWeight + x.SparseMin * request.SparseWeight +
                              (x.KeywordMatch ? 0 : request.KeywordWeight),
                DenseDistance = x.DenseMin,
                SparseDistance = x.SparseMin,
                KeywordMatch = x.KeywordMatch
            })
            .ToList();

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