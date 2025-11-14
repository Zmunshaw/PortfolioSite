using System.Linq.Expressions;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public interface IPageRepo
{
    Task AddPageAsync(Page page, CancellationToken cancellationToken = default);
    Task BatchAddPageAsync(IEnumerable<Page> pages, CancellationToken cancellationToken = default);

    Task<Page?> GetPageAsync(Expression<Func<Page, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Page>> GetPagesAsync(Expression<Func<Page, bool>> predicate, 
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Page>> GetPagesAsync(Expression<Func<Page, bool>> predicate, int take, int skip = 0, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Page>> GetPagesToCrawlAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    Task UpdatePageAsync(Page page, CancellationToken cancellationToken = default);
    Task BatchUpdatePageAsync(IEnumerable<Page> pages, CancellationToken cancellationToken = default);

    Task DeletePageAsync(Page page, CancellationToken cancellationToken = default);
}