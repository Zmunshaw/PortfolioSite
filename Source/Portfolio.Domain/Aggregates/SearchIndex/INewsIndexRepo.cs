using Portfolio.Common.Seedwork.Interfaces;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public interface INewsIndexRepo : IRepository<NewsIndex>
{
    Task<NewsIndex?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<NewsIndex?> GetByHostAsync(string host, CancellationToken ct = default);
}
