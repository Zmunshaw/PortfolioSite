using Portfolio.Common.Seedwork.Interfaces;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public interface IVideoIndexRepo : IRepository<VideoIndex>
{
    Task<VideoIndex?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<VideoIndex?> GetByHostAsync(string host, CancellationToken ct = default);
}
