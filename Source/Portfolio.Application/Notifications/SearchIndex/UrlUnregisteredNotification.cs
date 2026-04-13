using MediatR;

namespace Portfolio.Application.Notifications.SearchIndex;

public sealed record UrlUnregisteredNotification(
    Guid CatalogId,
    Guid EntryId,
    DateTime OccurredOn) : INotification;
