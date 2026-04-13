using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Domain.Aggregates.SearchIndex.Events;

public sealed record VideoIndexedEvent(
    Guid VideoIndexId,
    Guid DocumentId,
    string PageUrl,
    DateTime OccurredOn) : IDomainEvent
{
    public static VideoIndexedEvent Create(Guid videoIndexId, Guid documentId, string pageUrl)
        => new(videoIndexId, documentId, pageUrl, DateTime.UtcNow);
}
