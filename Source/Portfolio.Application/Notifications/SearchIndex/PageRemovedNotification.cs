using MediatR;

namespace Portfolio.Application.Notifications.SearchIndex;

public sealed record PageRemovedNotification(
    Guid PageIndexId,
    Guid DocumentId,
    DateTime OccurredOn) : INotification;
