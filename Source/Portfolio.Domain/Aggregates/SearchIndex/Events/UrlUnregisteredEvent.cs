using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Domain.Aggregates.SearchIndex.Events;

public sealed record UrlUnregisteredEvent(
    Guid CatalogId,
    Guid EntryId,
    DateTime OccurredOn) : IDomainEvent
{
    public static UrlUnregisteredEvent Create(Guid catalogId, Guid entryId)
        => new(catalogId, entryId, DateTime.UtcNow);
}
