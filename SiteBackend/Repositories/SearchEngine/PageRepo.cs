using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public class PageRepo : IPageRepo
{
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    private readonly ILogger<PageRepo> _logger;

    public PageRepo(ILogger<PageRepo> logger, IDbContextFactory<SearchEngineCtx> ctxFactory)
    {
        _logger = logger;
        _ctxFactory = ctxFactory;
    }

    public async Task AddPageAsync(Page page)
    {
        await using var ctx = await _ctxFactory.CreateDbContextAsync();
        // Avoid duplicates this way.
        await FindOrCreatePage(page, ctx);
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

    public async Task<Page?> GetPageAsync(Expression<Func<Page, bool>> predicate)
    {
        await using var ctx = await _ctxFactory.CreateDbContextAsync();
        return await ctx.Pages
            .Where(predicate)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Page>> GetPagesAsync(Expression<Func<Page, bool>> predicate)
    {
        var batchCtx = await _ctxFactory.CreateDbContextAsync();
        return await batchCtx.Pages.Where(predicate).ToListAsync();
    }

    public async Task<IEnumerable<Page>> GetPagesAsync(Expression<Func<Page, bool>> predicate, int take, int skip = 0)
    {
        var batchCtx = await _ctxFactory.CreateDbContextAsync();
        // TODO: convert to DTO for more perf, maybe; Less queries and stuff.
        return await batchCtx.Pages
            .AsNoTracking()
            .AsSplitQuery()
            .Where(predicate)
            .Skip(skip)
            .Take(take)
            .Include(p => p.Url)
            .Include(p => p.Content)
            .ThenInclude(c => c.Embeddings)
            .Include(p => p.Website)
            .ToListAsync();
    }

    public async Task UpdatePageAsync(Page page)
    {
        await using var ctx = await _ctxFactory.CreateDbContextAsync();
        ctx.Pages.Update(page);
    }

    public async Task BatchUpdatePageAsync(IEnumerable<Page> pages)
    {
        var enumerable = pages as Page[] ?? pages.ToArray();
        Dictionary<int, Page> updatedPageDict = new Dictionary<int, Page>();
        Dictionary<int, Page> dbPageDict;

        foreach (var page in enumerable)
        {
            _logger.LogDebug("Adding page: {Page} to dictionary", page.PageID);

            if (!updatedPageDict.ContainsKey(page.PageID))
            {
                updatedPageDict.Add(page.PageID, page);
            }
            else
                _logger.LogDebug("Already Exists {Page} in dictionary", page.PageID);
        }

        _logger.LogDebug("Updating {Page} pages", updatedPageDict.Count());
        var batchCtx = await _ctxFactory.CreateDbContextAsync();
        dbPageDict = await batchCtx.Pages
            .Where(dbp => updatedPageDict.Keys.Contains(dbp.PageID))
            .Include(dbp => dbp.Content)
            .Include(dbp => dbp.Website)
            .ToDictionaryAsync(dbp => dbp.PageID, dbp => dbp);

        if (dbPageDict.Count != updatedPageDict.Count)
        {
            _logger.LogWarning("Page count mismatch, 1 or more pages didn't exist in the database...");
            _logger.LogDebug("Updated Page count: {UpdatedCount}, Database Page count: {DatabaseCount}",
                dbPageDict.Count, updatedPageDict.Count);
        }

        foreach (var kvp in dbPageDict)
        {
            var dbPage = dbPageDict[kvp.Key];
            var updatedPage = updatedPageDict[kvp.Key];

            bool contentChanged = false;

            // Page
            if (updatedPage.LastCrawled != null && updatedPage.LastCrawled != dbPage.LastCrawled)
                dbPage.LastCrawled = updatedPage.LastCrawled;
            if (updatedPage.LastCrawlAttempt != null && updatedPage.LastCrawlAttempt != dbPage.LastCrawlAttempt)
                dbPage.LastCrawlAttempt = updatedPage.LastCrawlAttempt;

            // Content
            if (dbPage.Content != null)
            {
                // Proceed with the update
                if (updatedPage.Content.Title != dbPage.Content.Title)
                    dbPage.Content.Title = updatedPage.Content.Title;
                if (updatedPage.Content.Text != dbPage.Content.Text)
                {
                    dbPage.Content.Text = updatedPage.Content.Text;
                    contentChanged = true;
                }

                if (updatedPage.Content.ContentHash != dbPage.Content.ContentHash)
                    dbPage.Content.ContentHash = updatedPage.Content.ContentHash;
            }
            else
            {
                _logger.LogWarning("Content is null for PageID {PageID}", dbPage.PageID);
            }
        }

        var bulkConfig = new BulkConfig
        {
            PreserveInsertOrder = true,
            SetOutputIdentity = false,
        };

        await batchCtx.BulkUpdateAsync(dbPageDict.Values, bulkConfig);
        await batchCtx.SaveChangesAsync();
    }

    public Task DeletePageAsync(Page page)
    {
        throw new NotImplementedException();
    }

    public async Task SaveChangesAsync(bool clearCtxOnSave = true)
    {
        await using var ctx = await _ctxFactory.CreateDbContextAsync();
        if (!ctx.ChangeTracker.AutoDetectChangesEnabled)
            ctx.ChangeTracker.DetectChanges();

        await ctx.SaveChangesAsync();

        if (clearCtxOnSave)
            ctx.ChangeTracker.Clear(); // Avoid possible memleak from loaded entities.
    }

    public bool ToggleChangeTracker()
    {
        using var ctx = _ctxFactory.CreateDbContext();
        ctx.ChangeTracker.AutoDetectChangesEnabled = !ctx.ChangeTracker.AutoDetectChangesEnabled;

        return ctx.ChangeTracker.AutoDetectChangesEnabled;
    }

    public Task BatchDeletePageAsync(IEnumerable<Page> pages)
    {
        throw new NotImplementedException();
    }

    #region Helpers

    async Task<Page?> FindOrCreatePage(Page page, SearchEngineCtx ctx)
    {
        if (!Uri.TryCreate(page.Url.Location, UriKind.Absolute, out var pageUri))
        {
            _logger.LogError($"Invalid url: {page.Url} on page {page.Content?.Title}");
            return null;
        }

        var pageHost = pageUri.Host;
        var site = ctx.FindOrCreate(
            s => s.Host == pageHost,
            () => new Website(pageHost));

        var sitemap = ctx.FindOrCreate(
            sm => sm.Location == site.Host,
            () => new Sitemap { IsMapped = false, Location = site.Host });
        site.Sitemap = sitemap;

        var resPage = ctx.FindOrCreate(
            rp => (rp.Url.Location == pageUri.ToString() && rp.Website == site),
            () => new Page { Url = page.Url, Content = new Content(), Website = site });

        return resPage;
    }

    #endregion
}