using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Services;

public interface ICrawlerService
{
    Task UpdatePageAsync(Page page);
    Task BatchUpdatePagesAsync(IEnumerable<Page> pages);
    
    Task UpdateSitemapAsync(Page page);
    Task BatchUpdateSitemapsAsync(IEnumerable<Page> pages);
    
    Task<List<Page>> GetEmptyPagesAsync();
}