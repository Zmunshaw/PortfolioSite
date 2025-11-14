using SiteBackend.DTO;

namespace SiteBackend.Services;

public interface ICrawlerService
{
    Task UpdateCrawlerDataAsync(DTOCrawlerData page);
    Task BatchUpdateCrawlerDataAsync(IEnumerable<DTOCrawlerData> pages);

    Task<IEnumerable<DTOCrawlRequest>> GetEmptyPagesAsync(int amountToGet = 100);

    // Sitemap operations
    Task<DTOSitemapData> DiscoverSitemapAsync(string baseUrl, CancellationToken cancellationToken = default);
    Task<DTOSitemapData> DiscoverAndScrapeSitemapAsync(string baseUrl, int batchSize = 50, int maxConcurrent = 5, CancellationToken cancellationToken = default);
    Task SaveSitemapAsync(int websiteId, DTOSitemapData sitemapData, CancellationToken cancellationToken = default);
    Task CreatePagesFromSitemapAsync(int sitemapId, CancellationToken cancellationToken = default);
}