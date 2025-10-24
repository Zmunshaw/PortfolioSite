using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public class ContentRepo : IContentRepo
{
    private readonly ILogger<ContentRepo> _logger;
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
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

    public Task<Content?> GetContentAsync(Func<Content, bool> predicate)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Content>> GetContentsAsync(Func<Content, bool> predicate)
    {
        var batchCtx = await _ctxFactory.CreateDbContextAsync();
        
        var batchRes = await Task.Run(() => batchCtx.Contents
            .Include(ct => ct.Embeddings)
            .ThenInclude(emb => emb.Embedding)
            .Include(ct => ct.Embeddings)
            .Where(predicate));
        return batchRes;
    }

    public async Task<IEnumerable<Content>> GetContentsAsync(Func<Content, bool> predicate, int take, int skip = 0)
    {
        var batchCtx = await _ctxFactory.CreateDbContextAsync();
        
        var batchRes = await Task.Run(() => batchCtx.Contents
            .Include(ct => ct.Embeddings)
            .Where(predicate).Skip(skip).Take(take));
        return batchRes;
    }

    public async Task UpdateContentAsync(Content content)
    {
        throw new NotImplementedException();
    }

    public async Task BatchUpdateContentAsync(IEnumerable<Content> contents)
    {
        
        var batchCtx = await _ctxFactory.CreateDbContextAsync();
        
        var bulkConfig = new BulkConfig
        {
            PreserveInsertOrder = true,
            SetOutputIdentity = true,
            IncludeGraph = true,
        };
        
        await batchCtx.BulkInsertOrUpdateAsync(contents, bulkConfig);
        await batchCtx.SaveChangesAsync();
    }

    public Task DeleteContentAsync(Content content)
    {
        throw new NotImplementedException();
    }

    public Task SaveChangesAsync(bool clearCtxOnSave = true)
    {
        throw new NotImplementedException();
    }
}