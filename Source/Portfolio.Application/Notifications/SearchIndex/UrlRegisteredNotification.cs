using MediatR;

namespace Portfolio.Application.Notifications.SearchIndex;

public sealed record UrlRegisteredNotification(
    Guid CatalogId,
    Guid EntryId,
    string Url,
    int ContentTypeId,
    DateTime OccurredOn) : INotification;
