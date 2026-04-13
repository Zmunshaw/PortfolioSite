using Portfolio.Common.Seedwork.Interfaces;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public interface IPageIndexRepo : IRepository<PageIndex>
{
    Task<PageIndex?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PageIndex?> GetByHostAsync(string host, CancellationToken ct = default);
}
