using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Domain.Aggregates.Sitemap.Events;

public sealed record SitemapUrlAddedEvent(
    Guid SitemapId,
    Guid UrlId,
    string UrlLocation,
    DateTime OccurredOn) : IDomainEvent
{
    public static SitemapUrlAddedEvent Create(Guid sitemapId, Guid urlId, string urlLocation)
        => new(sitemapId, urlId, urlLocation, DateTime.UtcNow);
}
