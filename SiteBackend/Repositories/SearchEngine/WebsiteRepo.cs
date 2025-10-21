using Microsoft.EntityFrameworkCore;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public class WebsiteRepo : IWebsiteRepo
{
    private readonly ILogger<WebsiteRepo> _logger;
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    
    private SearchEngineCtx ctx;
    
    public WebsiteRepo(ILogger<WebsiteRepo> logger, IDbContextFactory<SearchEngineCtx> ctxFactory)
    {
        _logger = logger;
        _ctxFactory = ctxFactory;
        ctx = ctxFactory.CreateDbContext();
    }

    public async Task<IEnumerable<Website>> GetAllAsync()
    {
        _logger.LogInformation("Getting all websites");
        return await ctx.Websites.ToListAsync();
    }

    public async Task<Website?> GetByIdAsync(int id)
    {
        _logger.LogInformation($"Getting website with id {id}", id);
        return await ctx.Websites.FindAsync(id);
    }

    public async Task<Website?> GetByHostNameAsync(string hostName)
    {
        _logger.LogInformation("Getting website with hostname {hostName}", hostName);
        return await ctx.Websites
            .Where(site => site.Host.Equals(hostName, StringComparison.CurrentCultureIgnoreCase))
            .FirstOrDefaultAsync();
    }

    public async Task AddSitemapAsync(Sitemap sitemap)
    {
        _logger.LogDebug("Getting new dbCtx from factory...");
        _logger.LogInformation("Adding sitemap");
        string host = new Uri(sitemap.Location).Host;
        _logger.LogInformation($"parsed location:{sitemap.Location} to {host}");

        if (sitemap.UrlSet != null) 
            _logger.LogInformation($"Sitemap URLSET Length: {sitemap.UrlSet.Count}");
        if (sitemap.SitemapIndex != null)
            _logger.LogInformation($"Sitemap SITEMAPINDEX Length: {sitemap.SitemapIndex.Count}");

        Website newSite = new Website
        {
            Host = host,
            Sitemap = sitemap,
            Pages = new(),
        };
        _logger.LogInformation("Adding website");
        await ctx.Websites.AddAsync(newSite);
    }

    public async Task AddWebsiteAsync(Website website)
    {
        _logger.LogInformation("Adding website {website}", website);
        await ctx.Websites.AddAsync(website);
    }

    public void UpdateSitemap(Sitemap sitemap)
    {
        throw new NotImplementedException();
    }

    public void UpdateWebsite(Website website)
    {
        _logger.LogInformation("Updating website {website}", website);
        ctx.Websites.Update(website);
    }

    public void DeleteSitemap(Sitemap sitemap)
    {
        throw new NotImplementedException();
    }

    public void DeleteWebsite(Website website)
    {
        _logger.LogInformation("Deleting website {website}", website);
        ctx.Websites.Remove(website);
    }

    public async Task<bool> SaveChangesAsync()
    {
        _logger.LogDebug("Can Connect {CanConnect}, Connection String: {ConnStr}",
            await ctx.Database.CanConnectAsync(), ctx.Database.GetConnectionString());
        _logger.LogInformation("Saving changes");
        return await ctx.SaveChangesAsync() > 0;
    }
}