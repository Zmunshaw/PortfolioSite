using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Domain.Aggregates.Sitemap.Events;

public sealed record SitemapUrlRemovedEvent(
    Guid SitemapId,
    Guid UrlId,
    DateTime OccurredOn) : IDomainEvent
{
    public static SitemapUrlRemovedEvent Create(Guid sitemapId, Guid urlId)
        => new(sitemapId, urlId, DateTime.UtcNow);
}
