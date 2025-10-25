using SiteBackend.DTO;

namespace SiteBackend.Services;

public interface ICrawlerService
{
    Task UpdateCrawlerDataAsync(DTOCrawlerData page);
    Task BatchUpdateCrawlerDataAsync(IEnumerable<DTOCrawlerData> pages);

    Task<IEnumerable<DTOCrawlRequest>> GetEmptyPagesAsync(int amountToGet = 100);
}