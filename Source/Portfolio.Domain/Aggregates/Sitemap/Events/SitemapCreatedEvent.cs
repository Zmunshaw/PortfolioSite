using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Domain.Aggregates.Sitemap.Events;

public sealed record SitemapCreatedEvent(
    Guid SitemapId,
    string Location,
    DateTime OccurredOn) : IDomainEvent
{
    public static SitemapCreatedEvent Create(Guid sitemapId, string location)
        => new(sitemapId, location, DateTime.UtcNow);
}
