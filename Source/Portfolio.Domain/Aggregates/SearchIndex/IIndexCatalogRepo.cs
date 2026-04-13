using Portfolio.Common.Seedwork.Interfaces;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public interface IIndexCatalogRepo : IRepository<IndexCatalog>
{
    Task<IndexCatalog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IndexCatalog?> GetByHostAsync(string host, CancellationToken ct = default);
}
