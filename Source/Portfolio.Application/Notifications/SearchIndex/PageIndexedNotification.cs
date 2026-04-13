using MediatR;

namespace Portfolio.Application.Notifications.SearchIndex;

public sealed record PageIndexedNotification(
    Guid PageIndexId,
    Guid DocumentId,
    string Url,
    DateTime OccurredOn) : INotification;
