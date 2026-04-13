using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Roots.Sitemap.Enums;
using Portfolio.Domain.Roots.Sitemap.Events;
using Portfolio.Domain.Roots.Sitemap.ValueObjects;

namespace Portfolio.Domain.Roots.Sitemap;

public class Sitemap : AggregateRoot<Guid>
{
    private const int MaxUrlsPerSitemap = 50_000;

    private readonly List<SitemapUrl> _urls = [];
    private readonly List<Guid> _childSitemapIds = [];

    public SitemapLocation Location { get; private set; } = null!;
    public DateTime? LastModified { get; private set; }
    public bool IsMapped { get; private set; }
    public Guid? ParentSitemapId { get; private set; }

    public IReadOnlyList<SitemapUrl> Urls => _urls.AsReadOnly();
    public IReadOnlyList<Guid> ChildSitemapIds => _childSitemapIds.AsReadOnly();
    public bool IsSitemapIndex => _childSitemapIds.Count > 0;

    private Sitemap() { }

    public static Sitemap Create(string location, Guid? parentSitemapId = null)
    {
        var sitemap = new Sitemap
        {
            Id = Guid.NewGuid(),
            Location = new SitemapLocation(location),
            ParentSitemapId = parentSitemapId
        };

        sitemap.AddDomainEvent(
            SitemapCreatedEvent.Create(sitemap.Id, location));

        return sitemap;
    }

    public SitemapUrl AddUrl(
        string location,
        DateTime? lastModified = null,
        ChangeFrequency? changeFrequency = null,
        float priority = 0.5f)
    {
        if (_urls.Count >= MaxUrlsPerSitemap)
            throw new InvalidOperationException(
                $"Sitemap cannot exceed {MaxUrlsPerSitemap} URLs.");

        var loc = new SitemapLocation(location);
        if (_urls.Any(u => u.Location == loc))
            throw new InvalidOperationException(
                $"URL '{location}' already exists in this sitemap.");

        var url = new SitemapUrl(location, lastModified, changeFrequency, priority);
        _urls.Add(url);
        SetUpdated();

        AddDomainEvent(
            SitemapUrlAddedEvent.Create(Id, url.Id, location));

        return url;
    }

    public void RemoveUrl(Guid urlId)
    {
        var url = _urls.FirstOrDefault(u => u.Id == urlId)
            ?? throw new InvalidOperationException(
                $"URL with ID '{urlId}' not found in this sitemap.");

        _urls.Remove(url);
        SetUpdated();

        AddDomainEvent(
            SitemapUrlRemovedEvent.Create(Id, urlId));
    }

    public void AddChildSitemap(Guid childSitemapId)
    {
        if (childSitemapId == Id)
            throw new InvalidOperationException("A sitemap cannot reference itself as a child.");

        if (_childSitemapIds.Contains(childSitemapId))
            throw new InvalidOperationException(
                $"Child sitemap '{childSitemapId}' already exists in this index.");

        _childSitemapIds.Add(childSitemapId);
        SetUpdated();

        AddDomainEvent(
            SitemapChildAddedEvent.Create(Id, childSitemapId));
    }

    public void RemoveChildSitemap(Guid childSitemapId)
    {
        if (!_childSitemapIds.Remove(childSitemapId))
            throw new InvalidOperationException(
                $"Child sitemap '{childSitemapId}' not found in this index.");

        SetUpdated();
    }

    public void AddImageToUrl(Guid urlId, string imageLocation)
    {
        var url = FindUrl(urlId);
        url.AddImage(new ImageMedia(imageLocation));
        SetUpdated();
    }

    public void AddVideoToUrl(Guid urlId, VideoMedia video)
    {
        Guard.AgainstNull(video);
        var url = FindUrl(urlId);
        url.AddVideo(video);
        SetUpdated();
    }

    public void AddNewsToUrl(Guid urlId, NewsMedia news)
    {
        Guard.AgainstNull(news);
        var url = FindUrl(urlId);
        url.AddNews(news);
        SetUpdated();
    }

    public void AddAlternateLinkToUrl(Guid urlId, string hreflang, string href)
    {
        var url = FindUrl(urlId);
        url.AddAlternateLink(new AlternateLink(hreflang, href));
        SetUpdated();
    }

    public void MarkAsMapped()
    {
        IsMapped = true;
        SetUpdated();
    }

    public void UpdateLastModified(DateTime lastModified)
    {
        LastModified = lastModified;
        SetUpdated();
    }

    private SitemapUrl FindUrl(Guid urlId)
        => _urls.FirstOrDefault(u => u.Id == urlId)
           ?? throw new InvalidOperationException(
               $"URL with ID '{urlId}' not found in this sitemap.");
}
