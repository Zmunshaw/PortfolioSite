using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public interface IPageRepo
{
    Task AddPageAsync(Page page);
    Task BatchAddPageAsync(IEnumerable<Page> pages);
    
    Task<Page?> GetPageAsync(Func<Page, bool> predicate);
    Task<IEnumerable<Page>> GetPagesAsync(Func<Page, bool> predicate);
    Task<IEnumerable<Page>> GetPagesAsync(Func<Page, bool> predicate, int take, int skip = 0);
    
    Task UpdatePageAsync(Page page);
    Task BatchUpdatePageAsync(IEnumerable<Page> pages);
    
    Task DeletePageAsync(Page page);
    
    Task SaveChangesAsync(bool clearCtxOnSave = true);
}