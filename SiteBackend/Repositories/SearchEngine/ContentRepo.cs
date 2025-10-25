using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public class ContentRepo : IContentRepo
{
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    private readonly ILogger<ContentRepo> _logger;
    private SearchEngineCtx _ctx;

    public ContentRepo(ILogger<ContentRepo> logger, IDbContextFactory<SearchEngineCtx> ctxFactory)
    {
        _logger = logger;
        _ctxFactory = ctxFactory;
        _ctx = _ctxFactory.CreateDbContext();
    }

    public Task AddContentAsync(Content Content)
    {
        throw new NotImplementedException();
    }

    public Task BatchAddContentAsync(IEnumerable<Content> Contents)
    {
        throw new NotImplementedException();
    }

    public Task<Content?> GetContentAsync(Expression<Func<Content, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Content>> GetContentsAsync(Expression<Func<Content, bool>> predicate)
    {
        await using var batchCtx = await _ctxFactory.CreateDbContextAsync();

        return batchCtx.Contents
            .AsNoTracking()
            .Include(ct => ct.Embeddings)
            .Where(predicate)
            .ToList();
    }

    public async Task<IEnumerable<Content>> GetContentsAsync(Expression<Func<Content, bool>> predicate, int take,
        int skip = 0)
    {
        await using var batchCtx = await _ctxFactory.CreateDbContextAsync();

        return await batchCtx.Contents
            .AsNoTracking()
            .Where(predicate)
            .Skip(skip)
            .Take(take)
            .Include(ct => ct.Embeddings)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task UpdateContentAsync(Content content)
    {
        await using var ctx = await _ctxFactory.CreateDbContextAsync();

        ctx.Contents.Update(content);
        await ctx.SaveChangesAsync();
    }

    public async Task BatchUpdateContentAsync(IEnumerable<Content> contents)
    {
        await using var batchCtx = await _ctxFactory.CreateDbContextAsync();

        var bulkConfig = new BulkConfig
        {
            PreserveInsertOrder = true,
            SetOutputIdentity = true,
            IncludeGraph = true,
        };

        await batchCtx.BulkInsertOrUpdateAsync(contents.ToList(), bulkConfig);
    }

    public Task DeleteContentAsync(Content content)
    {
        throw new NotImplementedException();
    }

    public Task SaveChangesAsync(bool clearCtxOnSave = true)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<TextEmbedding>> GetSimilarEmbeddingsAsync(
        Vector queryVector,
        int limit = 25,
        double? maxDistance = null)
    {
        await using var ctx = await _ctxFactory.CreateDbContextAsync();

        var query = ctx.TextEmbeddings
            .OrderBy(te => te.Embedding.CosineDistance(queryVector))
            .Take(limit);

        if (maxDistance.HasValue)
            query = query.Where(te => te.Embedding.CosineDistance(queryVector) <= maxDistance.Value);

        return await query.ToListAsync();
    }
}