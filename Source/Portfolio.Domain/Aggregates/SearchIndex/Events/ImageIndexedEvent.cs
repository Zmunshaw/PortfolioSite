using Portfolio.Common.Seedwork.DomainEvents;

namespace Portfolio.Domain.Aggregates.SearchIndex.Events;

public sealed record ImageIndexedEvent(
    Guid ImageIndexId,
    Guid DocumentId,
    string PageUrl,
    string ImageUrl,
    DateTime OccurredOn) : IDomainEvent
{
    public static ImageIndexedEvent Create(Guid imageIndexId, Guid documentId, string pageUrl, string imageUrl)
        => new(imageIndexId, documentId, pageUrl, imageUrl, DateTime.UtcNow);
}
