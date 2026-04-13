using Portfolio.Common.Seedwork.Interfaces;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public interface IImageIndexRepo : IRepository<ImageIndex>
{
    Task<ImageIndex?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ImageIndex?> GetByHostAsync(string host, CancellationToken ct = default);
}
