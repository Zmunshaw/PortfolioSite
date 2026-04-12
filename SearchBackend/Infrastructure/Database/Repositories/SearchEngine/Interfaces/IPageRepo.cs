using System.Linq.Expressions;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public interface IPageRepo
{
    Task AddPageAsync(Page page);
    Task BatchAddPageAsync(IEnumerable<Page> pages);

    Task<Page?> GetPageAsync(Expression<Func<Page, bool>> predicate);
    Task<IEnumerable<Page>> GetPagesAsync(Expression<Func<Page, bool>> predicate);
    Task<IEnumerable<Page>> GetPagesAsync(Expression<Func<Page, bool>> predicate, int take, int skip = 0);

    Task<IEnumerable<Page>> GetPagesToCrawlAsync();
    Task UpdatePageAsync(Page page);
    Task BatchUpdatePageAsync(IEnumerable<Page> pages);

    Task DeletePageAsync(Page page);

    Task SaveChangesAsync(bool clearCtxOnSave = true);
}