using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;

namespace SiteBackend.Services;

public class CrawlerService : ICrawlerService
{
    private readonly ILogger<CrawlerService> _logger;
    private readonly IPageRepo _pageRepo;

    public CrawlerService(ILogger<CrawlerService> logger, IPageRepo pageRepo)
    {
        _logger = logger;
        _pageRepo = pageRepo;
    }
    
    public async Task UpdatePageAsync(Page page)
    {
        _logger.LogDebug("Updating page");
        await _pageRepo.UpdatePageAsync(page);
    }

    public async Task UpdateSitemapAsync(Page page)
    {
        throw new NotImplementedException();
    }

    public async Task BatchUpdateSitemapsAsync(IEnumerable<Page> pages)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Page>> GetEmptyPagesAsync()
    {
        return _pageRepo.GetPagesAsync(page =>
            page.Content?.Text == string.Empty || page.Content == null, 100).Result.ToList();
    }

    public async Task BatchUpdatePagesAsync(IEnumerable<Page> pages)
    {
        await _pageRepo.BatchUpdatePageAsync(pages);
    }
}