using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Services;

public interface ISitemapService
{
    Task AddSitemap(Sitemap sitemap);
}