using Mapster;
using Portfolio.Application.Notifications.SearchIndex;
using Portfolio.Domain.Aggregates.SearchIndex.Events;
using Portfolio.Domain.Aggregates.Sitemap.Events;

namespace Portfolio.Application.Notifications;

public static class DomainEventMappingConfig
{
    public static void Configure()
    {
        // SearchIndex events
        TypeAdapterConfig<PageIndexedEvent, PageIndexedNotification>.NewConfig();
        TypeAdapterConfig<PageRemovedEvent, PageRemovedNotification>.NewConfig();
        TypeAdapterConfig<ImageIndexedEvent, ImageIndexedNotification>.NewConfig();
        TypeAdapterConfig<VideoIndexedEvent, VideoIndexedNotification>.NewConfig();
        TypeAdapterConfig<NewsIndexedEvent, NewsIndexedNotification>.NewConfig();
        TypeAdapterConfig<UrlRegisteredEvent, UrlRegisteredNotification>.NewConfig();
        TypeAdapterConfig<UrlUnregisteredEvent, UrlUnregisteredNotification>.NewConfig();
    }

    private static readonly Dictionary<Type, Type> EventToNotificationMap = new()
    {
        [typeof(PageIndexedEvent)] = typeof(PageIndexedNotification),
        [typeof(PageRemovedEvent)] = typeof(PageRemovedNotification),
        [typeof(ImageIndexedEvent)] = typeof(ImageIndexedNotification),
        [typeof(VideoIndexedEvent)] = typeof(VideoIndexedNotification),
        [typeof(NewsIndexedEvent)] = typeof(NewsIndexedNotification),
        [typeof(UrlRegisteredEvent)] = typeof(UrlRegisteredNotification),
        [typeof(UrlUnregisteredEvent)] = typeof(UrlUnregisteredNotification),
    };

    public static Type? GetNotificationType(Type domainEventType)
        => EventToNotificationMap.GetValueOrDefault(domainEventType);
}
