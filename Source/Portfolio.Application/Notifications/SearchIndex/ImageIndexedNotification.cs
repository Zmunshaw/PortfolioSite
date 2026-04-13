using MediatR;

namespace Portfolio.Application.Notifications.SearchIndex;

public sealed record ImageIndexedNotification(
    Guid ImageIndexId,
    Guid DocumentId,
    string PageUrl,
    string ImageUrl,
    DateTime OccurredOn) : INotification;
