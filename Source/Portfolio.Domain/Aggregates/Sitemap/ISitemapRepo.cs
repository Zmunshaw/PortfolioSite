using Portfolio.Common.Seedwork.Interfaces;

namespace Portfolio.Domain.Aggregates.Sitemap;

public interface ISitemapRepo : IRepository<Sitemap>
{
    Task<Sitemap?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Sitemap?> GetByLocationAsync(string location, CancellationToken ct = default);
    Task<IReadOnlyList<Sitemap>> GetChildSitemapsAsync(Guid parentSitemapId, CancellationToken ct = default);
}
