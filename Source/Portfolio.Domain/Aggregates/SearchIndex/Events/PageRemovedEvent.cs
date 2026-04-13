using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Domain.Aggregates.SearchIndex.Events;

public sealed record PageRemovedEvent(
    Guid PageIndexId,
    Guid DocumentId,
    DateTime OccurredOn) : IDomainEvent
{
    public static PageRemovedEvent Create(Guid pageIndexId, Guid documentId)
        => new(pageIndexId, documentId, DateTime.UtcNow);
}
