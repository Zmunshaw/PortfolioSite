using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Registry;
using SearchBackend.Repositories.SearchEngine.Interfaces;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;

namespace SearchBackend.Repositories.SearchEngine;

/// <summary>
/// Repository for website and sitemap operations with resilience patterns.
/// Handles CRUD operations for websites and sitemaps with automatic retry logic via Polly.
/// </summary>
public class WebsiteRepo : IWebsiteRepo
{
    private readonly ILogger<WebsiteRepo> _logger;
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    private readonly ResiliencePipeline _resiliencePipeline;

    public WebsiteRepo(
        ILogger<WebsiteRepo> logger,
        IDbContextFactory<SearchEngineCtx> ctxFactory,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _logger = logger;
        _ctxFactory = ctxFactory;
        _resiliencePipeline = pipelineProvider.GetPipeline("db-backoff");
    }

    /// <summary>
    /// Gets all websites from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All websites with their sitemaps</returns>
    public async Task<IEnumerable<Website>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var websites = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                return await ctx.Websites
                    .AsNoTracking()
                    .Include(w => w.Sitemap)
                    .Include(w => w.Pages)
                    .ToListAsync(ct);
            }, cancellationToken);

            _logger.LogInformation("Retrieved {WebsiteCount} websites", websites.Count);
            return websites;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all websites after retries");
            throw;
        }
    }

    /// <summary>
    /// Gets a website by ID with full entity graph.
    /// </summary>
    /// <param name="id">Website ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Website if found, null otherwise</returns>
    public async Task<Website?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            _logger.LogWarning("GetByIdAsync called with invalid ID: {Id}", id);
            return null;
        }

        try
        {
            var website = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                return await ctx.Websites
                    .AsNoTracking()
                    .Include(w => w.Sitemap)
                    .Include(w => w.Pages)
                    .FirstOrDefaultAsync(w => w.WebsiteID == id, ct);
            }, cancellationToken);

            if (website != null)
                _logger.LogDebug("Retrieved website with ID {Id}: {Host}", id, website.Host);
            else
                _logger.LogDebug("Website with ID {Id} not found", id);

            return website;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get website by ID {Id} after retries", id);
            throw;
        }
    }

    /// <summary>
    /// Gets a website by hostname (case-insensitive).
    /// </summary>
    /// <param name="hostName">Hostname to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Website if found, null otherwise</returns>
    public async Task<Website?> GetByHostNameAsync(
        string hostName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            _logger.LogWarning("GetByHostNameAsync called with null or empty hostname");
            return null;
        }

        try
        {
            var website = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                return await ctx.Websites
                    .AsNoTracking()
                    .Include(w => w.Sitemap)
                    .Include(w => w.Pages)
                    .FirstOrDefaultAsync(w =>
                        w.Host.Equals(hostName, StringComparison.OrdinalIgnoreCase), ct);
            }, cancellationToken);

            if (website != null)
                _logger.LogDebug("Retrieved website by hostname: {HostName}", hostName);
            else
                _logger.LogDebug("Website with hostname {HostName} not found", hostName);

            return website;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get website by hostname {HostName} after retries", hostName);
            throw;
        }
    }

    /// <summary>
    /// Adds a new website to the database.
    /// </summary>
    /// <param name="website">Website to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddWebsiteAsync(Website website, CancellationToken cancellationToken = default)
    {
        if (website == null)
            throw new ArgumentNullException(nameof(website));

        if (string.IsNullOrWhiteSpace(website.Host))
        {
            _logger.LogWarning("AddWebsiteAsync called with website missing Host");
            throw new ArgumentException("Website Host cannot be null or empty", nameof(website));
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                await ctx.Websites.AddAsync(website, ct);
                await ctx.SaveChangesAsync(ct);
            }, cancellationToken);

            _logger.LogInformation("Added website: {Host}", website.Host);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            _logger.LogWarning(ex, "Website already exists: {Host}", website.Host);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add website {Host} after retries", website.Host);
            throw;
        }
    }

    /// <summary>
    /// Adds a sitemap to the database and creates its associated website if needed.
    /// </summary>
    /// <param name="sitemap">Sitemap to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddSitemapAsync(Sitemap sitemap, CancellationToken cancellationToken = default)
    {
        if (sitemap == null)
            throw new ArgumentNullException(nameof(sitemap));

        if (string.IsNullOrWhiteSpace(sitemap.Location))
        {
            _logger.LogWarning("AddSitemapAsync called with sitemap missing Location");
            throw new ArgumentException("Sitemap Location cannot be null or empty", nameof(sitemap));
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);

                // Extract host from sitemap location
                if (!Uri.TryCreate(sitemap.Location, UriKind.Absolute, out var uri))
                {
                    _logger.LogWarning("Invalid sitemap location URI: {Location}", sitemap.Location);
                    throw new ArgumentException($"Invalid sitemap location: {sitemap.Location}", nameof(sitemap));
                }

                var host = uri.Host;

                _logger.LogDebug("Extracted host {Host} from sitemap location", host);
                _logger.LogDebug("Sitemap contains {UrlCount} URLs and {SitemapCount} child sitemaps",
                    sitemap.UrlSet.Count, sitemap.SitemapIndex.Count);

                // Find or create website
                var website = ctx.FindOrCreate(
                    s => s.Host == host,
                    () => new Website { Host = host });

                sitemap.WebsiteID = website.WebsiteID;
                website.Sitemap = sitemap;

                await ctx.Sitemaps.AddAsync(sitemap, ct);
                await ctx.SaveChangesAsync(ct);
            }, cancellationToken);

            _logger.LogInformation("Added sitemap for host: {Host}", new Uri(sitemap.Location).Host);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add sitemap {Location} after retries", sitemap.Location);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing website.
    /// </summary>
    /// <param name="website">Website with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UpdateWebsiteAsync(Website website, CancellationToken cancellationToken = default)
    {
        if (website == null)
            throw new ArgumentNullException(nameof(website));

        if (website.WebsiteID <= 0)
        {
            _logger.LogWarning("UpdateWebsiteAsync called with invalid WebsiteID: {Id}", website.WebsiteID);
            throw new ArgumentException("Website must have a valid ID", nameof(website));
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                ctx.Websites.Update(website);
                await ctx.SaveChangesAsync(ct);
            }, cancellationToken);

            _logger.LogDebug("Updated website {Id}: {Host}", website.WebsiteID, website.Host);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating website {Id}", website.WebsiteID);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update website {Id} after retries", website.WebsiteID);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing sitemap.
    /// </summary>
    /// <param name="sitemap">Sitemap with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UpdateSitemapAsync(Sitemap sitemap, CancellationToken cancellationToken = default)
    {
        if (sitemap == null)
            throw new ArgumentNullException(nameof(sitemap));

        if (sitemap.SitemapID <= 0)
        {
            _logger.LogWarning("UpdateSitemapAsync called with invalid SitemapID: {Id}", sitemap.SitemapID);
            throw new ArgumentException("Sitemap must have a valid ID", nameof(sitemap));
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                ctx.Sitemaps.Update(sitemap);
                await ctx.SaveChangesAsync(ct);
            }, cancellationToken);

            _logger.LogDebug("Updated sitemap {Id}: {Location}", sitemap.SitemapID, sitemap.Location);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating sitemap {Id}", sitemap.SitemapID);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update sitemap {Id} after retries", sitemap.SitemapID);
            throw;
        }
    }

    /// <summary>
    /// Deletes a website and all associated data.
    /// </summary>
    /// <param name="website">Website to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteWebsiteAsync(Website website, CancellationToken cancellationToken = default)
    {
        if (website == null)
            throw new ArgumentNullException(nameof(website));

        if (website.WebsiteID <= 0)
        {
            _logger.LogWarning("DeleteWebsiteAsync called with invalid WebsiteID: {Id}", website.WebsiteID);
            throw new ArgumentException("Website must have a valid ID", nameof(website));
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);
                
                // Delete by ID to avoid tracking issues
                await ctx.Websites
                    .Where(w => w.WebsiteID == website.WebsiteID)
                    .ExecuteDeleteAsync(ct);
            }, cancellationToken);

            _logger.LogInformation("Deleted website {Id}: {Host}", website.WebsiteID, website.Host);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete website {Id} after retries", website.WebsiteID);
            throw;
        }
    }

    /// <summary>
    /// Deletes a sitemap and all associated URLs.
    /// </summary>
    /// <param name="sitemap">Sitemap to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteSitemapAsync(Sitemap sitemap, CancellationToken cancellationToken = default)
    {
        if (sitemap == null)
            throw new ArgumentNullException(nameof(sitemap));

        if (sitemap.SitemapID <= 0)
        {
            _logger.LogWarning("DeleteSitemapAsync called with invalid SitemapID: {Id}", sitemap.SitemapID);
            throw new ArgumentException("Sitemap must have a valid ID", nameof(sitemap));
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);

                // Delete by ID to avoid tracking issues
                await ctx.Sitemaps
                    .Where(s => s.SitemapID == sitemap.SitemapID)
                    .ExecuteDeleteAsync(ct);
            }, cancellationToken);

            _logger.LogInformation("Deleted sitemap {Id}: {Location}", sitemap.SitemapID, sitemap.Location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete sitemap {Id} after retries", sitemap.SitemapID);
            throw;
        }
    }

    /// <summary>
    /// Saves any pending changes to the database.
    /// Only needed for manual change tracking; most operations auto-save.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if changes were saved, false if nothing to save</returns>
    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var changeCount = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);

                // Test connection first
                var canConnect = await ctx.Database.CanConnectAsync(ct);
                if (!canConnect)
                {
                    _logger.LogWarning("Cannot connect to database");
                    return -1;
                }

                var changes = await ctx.SaveChangesAsync(ct);
                _logger.LogDebug("Saved {ChangeCount} changes to database", changes);
                return changes;
            }, cancellationToken);

            return changeCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes after retries");
            throw;
        }
    }
}