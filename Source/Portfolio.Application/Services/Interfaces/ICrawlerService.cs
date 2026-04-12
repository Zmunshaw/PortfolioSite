namespace Portfolio.Application.Services.Interfaces;

public interface ICrawlerService
{
    Task UpdateCrawlerDataAsync(DTOCrawlerData page);
    Task BatchUpdateCrawlerDataAsync(IEnumerable<DTOCrawlerData> pages);

    Task<IEnumerable<DTOCrawlRequest>> GetEmptyPagesAsync(int amountToGet = 100);
}