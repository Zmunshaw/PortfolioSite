using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public class PageRepo : IPageRepo
{
    private readonly ILogger<PageRepo> _logger;
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    private SearchEngineCtx _ctx;
    
    public PageRepo(ILogger<PageRepo> logger, IDbContextFactory<SearchEngineCtx> ctxFactory)
    {
        _logger = logger;
        _ctxFactory = ctxFactory;
        _ctx = _ctxFactory.CreateDbContext();
    }


    public bool ToggleChangeTracker()
    {
        _ctx.ChangeTracker.AutoDetectChangesEnabled = !_ctx.ChangeTracker.AutoDetectChangesEnabled;
        
        return _ctx.ChangeTracker.AutoDetectChangesEnabled;
    }

    public async Task AddPageAsync(Page page)
    {
        // Avoid duplicates this way.
        await FindOrCreatePage(page, _ctx);
        _logger.LogDebug("Added page: {Page}", page);
    }

    public async Task BatchAddPageAsync(IEnumerable<Page> pages)
    {
        var batchCtx = await _ctxFactory.CreateDbContextAsync();
        batchCtx.ChangeTracker.AutoDetectChangesEnabled = false;
        await Task.WhenAll(pages.Select(page => FindOrCreatePage(page, batchCtx)));
        batchCtx.ChangeTracker.DetectChanges();
        await batchCtx.SaveChangesAsync();
    }

    public async Task<Page?> GetPageAsync(Func<Page, bool> predicate)
    {
        return _ctx.Pages.Where(predicate).FirstOrDefault();
    }

    public async Task<IEnumerable<Page>> GetAllPagesAsync(Func<Page, bool> predicate)
    {
        var batchCtx = await _ctxFactory.CreateDbContextAsync();
        
        // Maybe this actually works and we offload to another thread but vanilla EF blows... so....
        // TODO: Fix threading issues, probably
        return await Task.Run(() => batchCtx.Pages.Where(predicate).ToArray());
    }
    
    public async Task UpdatePageAsync(Page page)
    {
        _ctx.Pages.Update(page);
    }

    public async Task BatchUpdatePageAsync(IEnumerable<Page> pages)
    {
        var batchCtx = await _ctxFactory.CreateDbContextAsync();
        batchCtx.ChangeTracker.AutoDetectChangesEnabled = false;
        await Task.WhenAll(pages.Select(page => FindOrCreatePage(page, batchCtx)));
        batchCtx.ChangeTracker.DetectChanges();
        await batchCtx.SaveChangesAsync();
    }

    public Task DeletePageAsync(Page page)
    {
        throw new NotImplementedException();
    }
    public Task BatchDeletePageAsync(IEnumerable<Page> pages)
    {
        throw new NotImplementedException();
    }
    
    public async Task SaveChangesAsync(bool clearCtxOnSave = true)
    {
        if (!_ctx.ChangeTracker.AutoDetectChangesEnabled)
            _ctx.ChangeTracker.DetectChanges();
        
        await _ctx.SaveChangesAsync();
        
        if (clearCtxOnSave)
            _ctx.ChangeTracker.Clear(); // Avoid possible memleak from loaded entities.
    }
    #region Helpers

    async Task<Page?> FindOrCreatePage(Page page, SearchEngineCtx ctx)
    {
        if (!Uri.TryCreate(page.Url, UriKind.Absolute, out var pageUri))
        {
            _logger.LogError($"Invalid url: {page.Url} on page {page.Content?.Title}");
            return null;
        }
        
        var pageHost = pageUri.Host;
        var site = ctx.FindOrCreate(
            s => s.Host == pageHost,
            () => new Website {Host = pageHost, Pages = []});
        var sitemap = ctx.FindOrCreate(
            sm => sm.Location == site.Host,
            () => new Sitemap { IsMapped = false, Location = site.Host });
        site.Sitemap = sitemap;
        
        var resPage = ctx.FindOrCreate(
            rp => (rp.Url == pageUri.ToString() && rp.Website == site),
            () => new Page {Url = page.Url, Content = new Content(), Website = site});
        
        return resPage;
    }
    #endregion
}