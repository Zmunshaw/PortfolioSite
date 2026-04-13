using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Domain.Aggregates.SearchIndex.Events;

public sealed record UrlRegisteredEvent(
    Guid CatalogId,
    Guid EntryId,
    string Url,
    int ContentTypeId,
    DateTime OccurredOn) : IDomainEvent
{
    public static UrlRegisteredEvent Create(Guid catalogId, Guid entryId, string url, int contentTypeId)
        => new(catalogId, entryId, url, contentTypeId, DateTime.UtcNow);
}
