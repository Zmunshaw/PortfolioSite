using SiteBackend.DTO;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Services;

public interface ICrawlerService
{
    Task UpdateCrawlerDataAsync(DTOCrawlerData page);
    Task BatchUpdateCrawlerDataAsync(IEnumerable<DTOCrawlerData> pages);
    
    Task<IEnumerable<DTOCrawlRequest>> GetEmptyPagesAsync(int amountToGet = 100);
}