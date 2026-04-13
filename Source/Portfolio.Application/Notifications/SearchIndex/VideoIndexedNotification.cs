using MediatR;

namespace Portfolio.Application.Notifications.SearchIndex;

public sealed record VideoIndexedNotification(
    Guid VideoIndexId,
    Guid DocumentId,
    string PageUrl,
    DateTime OccurredOn) : INotification;
