using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Services;

public class SitemapService : ISitemapService
{
    ILogger<SitemapService> _logger;
    private IWebsiteRepo _repo;
    
    public SitemapService(ILogger<SitemapService> logger,  IWebsiteRepo repo)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task AddSitemap(Sitemap sitemap)
    {
        _logger.LogInformation("Adding sitemap");
        await _repo.AddSitemapAsync(sitemap);
        _logger.LogInformation("Committing changes to sitemap...");
        await _repo.SaveChangesAsync();
    }
}