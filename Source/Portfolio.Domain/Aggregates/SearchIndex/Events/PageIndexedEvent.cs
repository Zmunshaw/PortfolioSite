using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Domain.Aggregates.SearchIndex.Events;

public sealed record PageIndexedEvent(
    Guid PageIndexId,
    Guid DocumentId,
    string Url,
    DateTime OccurredOn) : IDomainEvent
{
    public static PageIndexedEvent Create(Guid pageIndexId, Guid documentId, string url)
        => new(pageIndexId, documentId, url, DateTime.UtcNow);
}
