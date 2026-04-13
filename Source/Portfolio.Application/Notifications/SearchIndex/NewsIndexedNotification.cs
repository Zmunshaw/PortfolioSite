using MediatR;

namespace Portfolio.Application.Notifications.SearchIndex;

public sealed record NewsIndexedNotification(
    Guid NewsIndexId,
    Guid DocumentId,
    string PageUrl,
    DateTime OccurredOn) : INotification;
