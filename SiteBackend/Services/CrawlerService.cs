using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using SiteBackend.Database;
using SiteBackend.DTO;
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
    
    public async Task UpdateCrawlerDataAsync(DTOCrawlerData dtoPage)
    {
        _logger.LogDebug("Updating page");
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<DTOCrawlRequest>> GetEmptyPagesAsync(int amountToGet = 100)
    {
        // TODO: Add more robust logic for determining valid crawl candidates.
        var validPages = _pageRepo.GetPagesAsync(page => page.LastCrawlAttempt == null, amountToGet)
            .Result.ToList();
        
        foreach (var page in validPages)
            page.LastCrawlAttempt = DateTime.UtcNow;
        
        await _pageRepo.BatchUpdatePageAsync(validPages);
        
        var dtoPages = validPages.Select(pg => new DTOCrawlRequest(pg)).ToList();
        return dtoPages;
    }

    public async Task BatchUpdateCrawlerDataAsync(IEnumerable<DTOCrawlerData> pages)
    {
        var dtoCrawlerPages = pages as DTOCrawlerData[] ?? pages.ToArray();
        var pageList = dtoCrawlerPages.Select(pg => pg.ConvertToPage()).ToList();
        await _pageRepo.BatchUpdatePageAsync(pageList);
    }
}