using Microsoft.EntityFrameworkCore;
using SearchBackend.Repositories.SearchEngine.Interfaces;
using SiteBackend.Database;
using SiteBackend.Services;

namespace SiteBackend.Singletons;

/// <summary>
/// Background service that automatically discovers and processes sitemaps for websites in the database.
/// Runs on a configurable interval and handles sitemap discovery, parsing, and page creation.
/// </summary>
public class SitemapCrawlManager : BackgroundService
{
    private readonly int _intervalMinutes = int.Parse(
        Environment.GetEnvironmentVariable("SITEMAP_CRAWL_INTERVAL_MINUTES") ?? "1");

    private readonly ILogger<SitemapCrawlManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public SitemapCrawlManager(
        IServiceScopeFactory scopeFactory,
        ILogger<SitemapCrawlManager> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Manager} running at: {Time}. Interval: {Interval} minutes",
            GetType().Name, DateTimeOffset.Now, _intervalMinutes);

        // Wait a bit before starting to allow other services to initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DiscoverAndMapSitemapsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SitemapCrawlManager");
            }

            // Wait for the configured interval before next cycle
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("SitemapCrawlManager shutting down");
                break;
            }
        }
    }

    private async Task DiscoverAndMapSitemapsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var websiteRepo = scope.ServiceProvider.GetRequiredService<IWebsiteRepo>();
        var crawlerService = scope.ServiceProvider.GetRequiredService<ICrawlerService>();
        var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchEngineCtx>>();

        // Get websites that need sitemap discovery
        var websitesNeedingMapping = await GetWebsitesNeedingMappingAsync(websiteRepo, ctxFactory, stoppingToken);

        if (!websitesNeedingMapping.Any())
        {
            _logger.LogInformation("No websites need sitemap discovery");
            return;
        }

        _logger.LogInformation("Found {Count} websites needing sitemap discovery", websitesNeedingMapping.Count);

        foreach (var website in websitesNeedingMapping)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                await DiscoverAndMapSingleSitemap(website, crawlerService, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover sitemap for website {WebsiteId}: {Host}",
                    website.WebsiteID, website.Host);
            }

            // Small delay between sitemap operations to avoid overwhelming the crawler service
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }

        _logger.LogInformation("Sitemap discovery cycle completed");
    }

    private async Task DiscoverAndMapSingleSitemap(
        SiteBackend.Models.SearchEngine.Index.Website website,
        ICrawlerService crawlerService,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Discovering sitemap for website {WebsiteId}: {Host}",
            website.WebsiteID, website.Host);

        try
        {
            // Ensure the host has a protocol
            var baseUrl = website.Host.StartsWith("http") ? website.Host : $"https://{website.Host}";

            // Discover the sitemap
            var sitemapData = await crawlerService.DiscoverSitemapAsync(baseUrl, stoppingToken);

            if (sitemapData == null)
            {
                _logger.LogWarning("No sitemap found for {Host}", website.Host);
                return;
            }

            // Save the sitemap to database
            await crawlerService.SaveSitemapAsync(website.WebsiteID, sitemapData, stoppingToken);

            // Get the sitemap ID from the database
            using var scope = _scopeFactory.CreateScope();
            var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchEngineCtx>>();
            await using var ctx = await ctxFactory.CreateDbContextAsync(stoppingToken);

            var sitemap = await ctx.Sitemaps
                .Where(s => s.WebsiteID == website.WebsiteID)
                .OrderByDescending(s => s.SitemapID)
                .FirstOrDefaultAsync(stoppingToken);

            if (sitemap != null)
            {
                // Create pages from the sitemap URLs
                await crawlerService.CreatePagesFromSitemapAsync(sitemap.SitemapID, stoppingToken);

                // Mark sitemap as mapped
                sitemap.IsMapped = true;
                await ctx.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "Successfully discovered and mapped sitemap for {Host}: {UrlCount} URLs",
                    website.Host, sitemapData.UrlSet.Count);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error discovering sitemap for {Host}. Will retry later.", website.Host);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error discovering sitemap for {Host}", website.Host);
            throw;
        }
    }

    private async Task<List<SiteBackend.Models.SearchEngine.Index.Website>> GetWebsitesNeedingMappingAsync(
        IWebsiteRepo websiteRepo,
        IDbContextFactory<SearchEngineCtx> ctxFactory,
        CancellationToken cancellationToken)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync(cancellationToken);

        // Get websites where:
        // - Sitemap is null (never mapped)
        // - OR sitemap exists but IsMapped is false (mapping failed or incomplete)
        // - OR sitemap exists but hasn't been updated in 7 days
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var websites = await ctx.Websites
            .Include(w => w.Sitemap)
            .Where(w =>
                w.Sitemap == null ||
                w.Sitemap.IsMapped == false ||
                (w.Sitemap.LastModified.HasValue && w.Sitemap.LastModified.Value < sevenDaysAgo))
            .Take(10) // Process max 10 websites per cycle
            .ToListAsync(cancellationToken);

        return websites;
    }
}
