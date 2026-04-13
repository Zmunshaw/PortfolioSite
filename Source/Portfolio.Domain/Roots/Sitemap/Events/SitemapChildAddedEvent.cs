using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Domain.Roots.Sitemap.Events;

public sealed record SitemapChildAddedEvent(
    Guid ParentSitemapId,
    Guid ChildSitemapId,
    DateTime OccurredOn) : IDomainEvent
{
    public static SitemapChildAddedEvent Create(Guid parentSitemapId, Guid childSitemapId)
        => new(parentSitemapId, childSitemapId, DateTime.UtcNow);
}
