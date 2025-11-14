using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Registry;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;

namespace SearchBackend.Repositories.SearchEngine;

/// <summary>
/// Repository for page operations with resilience patterns.
/// Handles CRUD operations for pages with automatic retry logic via Polly.
/// </summary>
public class PageRepo : IPageRepo
{
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    private readonly ILogger<PageRepo> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    // MARKED: for #30
    private const int DaysSinceLastCrawl = 31;
    private const int HoursSinceLastAttempt = 5;
    private const int MaxCrawlAttempts = 5;
    private const int DefaultBatchSize = 20;

    public PageRepo(ILogger<PageRepo> logger, IDbContextFactory<SearchEngineCtx> ctxFactory,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _logger = logger;
        _ctxFactory = ctxFactory;
        _resiliencePipeline = pipelineProvider.GetPipeline("db-backoff");
    }

    /// <summary>
    /// Adds a single page, automatically handling duplicates.
    /// Creates associated website and sitemap if they don't exist.
    /// </summary>
    public async Task AddPageAsync(Page page, CancellationToken cancellationToken = default)
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                await FindOrCreatePage(page, ctx);
                await ctx.SaveChangesAsync(ct);
            }, cancellationToken);

            _logger.LogDebug("Added page: {PageUrl}", page.Url?.Location ?? "unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add page: {PageUrl}", page.Url?.Location ?? "unknown");
            throw;
        }
    }

    /// <summary>
    /// Batch adds multiple pages with optimized change tracking.
    /// Automatically deduplicates based on URL and host.
    /// </summary>
    public async Task BatchAddPageAsync(IEnumerable<Page> pages, CancellationToken cancellationToken = default)
    {
        var pageList = pages as Page[] ?? pages.ToArray();
        if (pageList.Length == 0)
        {
            _logger.LogWarning("BatchAddPageAsync called with empty collection");
            return;
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                ctx.ChangeTracker.AutoDetectChangesEnabled = false;

                try
                {
                    await Task.WhenAll(pageList.Select(page => FindOrCreatePage(page, ctx)));
                    ctx.ChangeTracker.DetectChanges();
                    await ctx.SaveChangesAsync(ct);
                }
                finally
                {
                    ctx.ChangeTracker.AutoDetectChangesEnabled = true;
                }
            }, cancellationToken);

            _logger.LogInformation("Batch added {PageCount} pages", pageList.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch add {PageCount} pages", pageList.Length);
            throw;
        }
    }

    /// <summary>
    /// Gets a single page matching the predicate.
    /// </summary>
    public async Task<Page?> GetPageAsync(Expression<Func<Page, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                return await ctx.Pages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(predicate, ct);
            }, cancellationToken);

            _logger.LogDebug("Retrieved page matching predicate");
            return page;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get page after retries");
            throw;
        }
    }

    /// <summary>
    /// Gets all pages matching the predicate.
    /// </summary>
    public async Task<IEnumerable<Page>> GetPagesAsync(Expression<Func<Page, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pages = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                return await ctx.Pages
                    .AsNoTracking()
                    .Where(predicate)
                    .ToListAsync(ct);
            }, cancellationToken);

            _logger.LogDebug("Retrieved {PageCount} pages matching predicate", pages.Count);
            return pages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pages after retries");
            throw;
        }
    }

    /// <summary>
    /// Gets paginated pages matching the predicate with full entity graph.
    /// Includes Url, Content, Website, and Embeddings.
    /// </summary>
    public async Task<IEnumerable<Page>> GetPagesAsync(Expression<Func<Page, bool>> predicate, int take, int skip = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pages = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                return await ctx.Pages
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(predicate)
                    .Include(p => p.Url)
                    .Include(p => p.Content)
                    .ThenInclude(c => c!.Embeddings)
                    .Include(p => p.Website)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(ct);
            }, cancellationToken);

            _logger.LogDebug("Retrieved {PageCount} pages with pagination (skip={Skip}, take={Take})",
                pages.Count, skip, take);
            return pages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get paginated pages after retries");
            throw;
        }
    }

    /// <summary>
    /// Gets pages that need to be crawled based on crawl history.
    /// Returns pages that:
    /// - Have never been crawled
    /// - Haven't been crawled in 31+ days
    /// - Last crawl attempt was 5+ hours ago and fewer than 5 attempts made
    /// </summary>
    public async Task<IEnumerable<Page>> GetPagesToCrawlAsync(int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pages = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                var utcNow = DateTime.UtcNow;

                return await ctx.Pages
                    .Where(p =>
                        p.LastCrawled == null ||
                        (p.LastCrawled < utcNow.AddDays(-DaysSinceLastCrawl) &&
                         p.LastCrawlAttempt == null) ||
                        (p.LastCrawlAttempt < utcNow.AddHours(-HoursSinceLastAttempt) &&
                         p.CrawlAttempts < MaxCrawlAttempts))
                    .Include(p => p.Content)
                    .Include(p => p.Website)
                    .Include(p => p.Url)
                    .Include(p => p.Outlinks)
                    .OrderByDescending(p => p.Url != null ? p.Url.Priority : 0.5f)
                    .ThenBy(p => p.LastCrawled ?? DateTime.MinValue)
                    .Take(batchSize)
                    .ToListAsync(ct);
            }, cancellationToken);

            _logger.LogInformation("Retrieved {PageCount} pages to crawl", pages.Count);
            return pages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pages to crawl after retries");
            throw;
        }
    }

    /// <summary>
    /// Updates a single page.
    /// </summary>
    public async Task UpdatePageAsync(Page page, CancellationToken cancellationToken = default)
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                ctx.Pages.Update(page);
                await ctx.SaveChangesAsync(ct);
            }, cancellationToken);

            _logger.LogDebug("Updated page: {PageId}", page.PageID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update page {PageId} after retries", page.PageID);
            throw;
        }
    }

    /// <summary>
    /// Batch updates multiple pages with intelligent merging.
    /// Fetches existing pages from DB and merges updates, avoiding overwrites of unmodified fields.
    /// </summary>
    public async Task BatchUpdatePageAsync(IEnumerable<Page> pages, CancellationToken cancellationToken = default)
    {
        var pageList = pages as Page[] ?? pages.ToArray();
        if (pageList.Length == 0)
        {
            _logger.LogWarning("BatchUpdatePageAsync called with empty collection");
            return;
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);

                // Build dictionary of incoming updates
                var updatedPageDict = pageList
                    .DistinctBy(p => p.PageID)
                    .ToDictionary(p => p.PageID, p => p);

                _logger.LogDebug("Batch updating {PageCount} unique pages", updatedPageDict.Count);

                // Fetch existing pages from database
                var dbPageDict = await ctx.Pages
                    .Where(p => updatedPageDict.Keys.Contains(p.PageID))
                    .Include(p => p.Content)
                    .Include(p => p.Website)
                    .ToDictionaryAsync(p => p.PageID, p => p, ct);

                if (dbPageDict.Count != updatedPageDict.Count)
                {
                    var missingCount = updatedPageDict.Count - dbPageDict.Count;
                    _logger.LogWarning(
                        "Page count mismatch: {MissingCount} pages not found in database",
                        missingCount);
                }
                
                foreach (var (pageId, updatedPage) in updatedPageDict)
                {
                    if (!dbPageDict.TryGetValue(pageId, out var dbPage))
                    {
                        _logger.LogWarning("Page {PageId} not found in database, skipping", pageId);
                        continue;
                    }
                    
                    if (updatedPage.LastCrawled.HasValue && updatedPage.LastCrawled != dbPage.LastCrawled)
                        dbPage.LastCrawled = updatedPage.LastCrawled;

                    if (updatedPage.LastCrawlAttempt.HasValue &&
                        updatedPage.LastCrawlAttempt != dbPage.LastCrawlAttempt)
                        dbPage.LastCrawlAttempt = updatedPage.LastCrawlAttempt;

                    if (updatedPage.CrawlAttempts > dbPage.CrawlAttempts)
                        dbPage.CrawlAttempts = updatedPage.CrawlAttempts;
                    
                    if (dbPage.Content != null && updatedPage.Content != null)
                    {
                        if (!string.IsNullOrEmpty(updatedPage.Content.Title))
                            dbPage.Content.Title = updatedPage.Content.Title;

                        if (!string.IsNullOrEmpty(updatedPage.Content.Text))
                            dbPage.Content.Text = updatedPage.Content.Text;

                        if (!string.IsNullOrEmpty(updatedPage.Content.ContentHash))
                            dbPage.Content.ContentHash = updatedPage.Content.ContentHash;
                    }
                }

                var bulkConfig = new BulkConfig
                {
                    PreserveInsertOrder = true,
                    SetOutputIdentity = false,
                };

                await ctx.BulkUpdateAsync(dbPageDict.Values, bulkConfig, cancellationToken: ct);
                await ctx.SaveChangesAsync(ct);
            }, cancellationToken);

            _logger.LogInformation("Batch updated {PageCount} pages", pageList.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch update {PageCount} pages after retries", pageList.Length);
            throw;
        }
    }

    /// <summary>
    /// Deletes a single page.
    /// </summary>
    public async Task DeletePageAsync(Page page, CancellationToken cancellationToken = default)
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                ctx.Pages.Remove(page);
                await ctx.SaveChangesAsync(true, ct);
            }, cancellationToken);

            _logger.LogDebug("Deleted page: {PageId}", page.PageID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete page {PageId} after retries", page.PageID);
            throw;
        }
    }

    /// <summary>
    /// Batch deletes multiple pages.
    /// </summary>
    public async Task BatchDeletePageAsync(IEnumerable<Page> pages, CancellationToken cancellationToken = default)
    {
        var pageList = pages as Page[] ?? pages.ToArray();
        if (pageList.Length == 0)
        {
            _logger.LogWarning("BatchDeletePageAsync called with empty collection");
            return;
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);

                var pageIds = pageList.Select(p => p.PageID).ToList();

                // Delete by ID to avoid tracking issues
                await ctx.Pages
                    .Where(p => pageIds.Contains(p.PageID))
                    .ExecuteDeleteAsync(ct);
            }, cancellationToken);

            _logger.LogInformation("Batch deleted {PageCount} pages", pageList.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch delete {PageCount} pages after retries", pageList.Length);
            throw;
        }
    }
    
    /// <summary>
    /// Helper method to find or create a page with its associated website and sitemap.
    /// Handles URL validation and entity creation atomically within the context.
    /// </summary>
    private async Task<Page?> FindOrCreatePage(Page page, SearchEngineCtx ctx)
    {
        if (page?.Url?.Location == null)
        {
            _logger.LogWarning("Page or URL is null, skipping");
            return null;
        }

        if (!Uri.TryCreate(page.Url.Location, UriKind.Absolute, out var pageUri))
        {
            _logger.LogError("Invalid URL: {Url} on page {PageTitle}",
                page.Url.Location, page.Content?.Title ?? "unknown");
            return null;
        }

        try
        {
            var pageHost = pageUri.Host;
            
            var site = ctx.FindOrCreate(
                s => s.Host == pageHost,
                () => new Website(pageHost));
            
            var sitemap = ctx.FindOrCreate(
                sm => sm.Location == site.Host && sm.WebsiteID == site.WebsiteID,
                () => new Sitemap { IsMapped = false, Location = site.Host, Website = site });

            site.Sitemap = sitemap;
            
            var resPage = ctx.FindOrCreate(
                p => p.Url != null && p.Url.Location == pageUri.ToString() && p.WebsiteID == site.WebsiteID,
                () => new Page
                {
                    Url = new Url(pageUri.ToString(), sitemap, null),
                    Content = new Content(),
                    Website = site
                });

            return resPage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FindOrCreatePage for URL {Url}", page.Url.Location);
            throw;
        }
    }
}