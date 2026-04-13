using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.Sitemap.Enums;
using Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

namespace Portfolio.Domain.Aggregates.Sitemap;

public class SitemapUrl : Entity<Guid>
{
    private readonly List<Url> _images = [];
    private readonly List<VideoMedia> _videos = [];
    private readonly List<NewsMedia> _news = [];
    private readonly List<AlternateLink> _alternateLinks = [];

    public Url Location { get; private set; } = null!;
    public DateTime? LastModified { get; private set; }
    public ChangeFrequency? ChangeFrequency { get; private set; }
    public UrlPriority Priority { get; private set; } = null!;
    public CrawlState CrawlState { get; private set; } = CrawlState.Initial;

    public IReadOnlyList<Url> Images => _images.AsReadOnly();
    public IReadOnlyList<VideoMedia> Videos => _videos.AsReadOnly();
    public IReadOnlyList<NewsMedia> News => _news.AsReadOnly();
    public IReadOnlyList<AlternateLink> AlternateLinks => _alternateLinks.AsReadOnly();

    private SitemapUrl() { }

    internal SitemapUrl(
        string location,
        DateTime? lastModified = null,
        ChangeFrequency? changeFrequency = null,
        float priority = 0.5f)
    {
        Id = Guid.NewGuid();
        Location = new Url(location);
        LastModified = lastModified;
        ChangeFrequency = changeFrequency;
        Priority = new UrlPriority(priority);
    }

    internal void RecordCrawlAttempt()
    {
        CrawlState = CrawlState.WithAttempt(DateTime.UtcNow);
        SetUpdated();
    }

    internal void RecordCrawlSuccess()
    {
        CrawlState = CrawlState.WithSuccess(DateTime.UtcNow);
        SetUpdated();
    }

    public bool IsDue(DateTime asOf) => CrawlState.IsDue(asOf, ChangeFrequency);

    internal void AddImage(Url image)
    {
        Guard.AgainstNull(image);
        _images.Add(image);
        SetUpdated();
    }

    internal void RemoveImage(Url image)
    {
        _images.Remove(image);
        SetUpdated();
    }

    internal void AddVideo(VideoMedia video)
    {
        Guard.AgainstNull(video);
        _videos.Add(video);
        SetUpdated();
    }

    internal void RemoveVideo(VideoMedia video)
    {
        _videos.Remove(video);
        SetUpdated();
    }

    internal void AddNews(NewsMedia news)
    {
        Guard.AgainstNull(news);
        _news.Add(news);
        SetUpdated();
    }

    internal void RemoveNews(NewsMedia news)
    {
        _news.Remove(news);
        SetUpdated();
    }

    internal void AddAlternateLink(AlternateLink link)
    {
        Guard.AgainstNull(link);
        _alternateLinks.Add(link);
        SetUpdated();
    }

    internal void RemoveAlternateLink(AlternateLink link)
    {
        _alternateLinks.Remove(link);
        SetUpdated();
    }
}
