using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Domain.Aggregates.SearchIndex.Events;

public sealed record NewsIndexedEvent(
    Guid NewsIndexId,
    Guid DocumentId,
    string PageUrl,
    DateTime OccurredOn) : IDomainEvent
{
    public static NewsIndexedEvent Create(Guid newsIndexId, Guid documentId, string pageUrl)
        => new(newsIndexId, documentId, pageUrl, DateTime.UtcNow);
}
