using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Errors;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Errors;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.Sitemap.Enums;
using Portfolio.Domain.Aggregates.Sitemap.Events;
using Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

namespace Portfolio.Domain.Aggregates.Sitemap;

public class Sitemap : AggregateRoot<Guid>
{
    public static class Errors
    {
        public static Error UrlLimitExceeded(int max) =>
            new("SITEMAP_URL_LIMIT_EXCEEDED", $"Sitemap cannot exceed {max} URLs.");

        public static Error DuplicateUrl(string location) =>
            new("SITEMAP_DUPLICATE_URL", $"URL '{location}' already exists in this sitemap.");

        public static Error UrlNotFound(Guid urlId) =>
            new("SITEMAP_URL_NOT_FOUND", $"URL with ID '{urlId}' not found in this sitemap.");

        public static Error SelfReferencingChild =>
            new("SITEMAP_SELF_REFERENCING_CHILD", "A sitemap cannot reference itself as a child.");

        public static Error DuplicateChild(SitemapId childId) =>
            new("SITEMAP_DUPLICATE_CHILD", $"Child sitemap '{childId}' already exists in this index.");

        public static Error ChildNotFound(SitemapId childId) =>
            new("SITEMAP_CHILD_NOT_FOUND", $"Child sitemap '{childId}' not found in this index.");

        public static Error UrlHostMismatch(string urlHost, string sitemapHost) =>
            new("SITEMAP_URL_HOST_MISMATCH", $"URL host '{urlHost}' does not match sitemap host '{sitemapHost}'.");

        public static Error ImageLimitExceeded(Guid urlId) =>
            new("SITEMAP_IMAGE_LIMIT_EXCEEDED", $"URL '{urlId}' cannot have more than 1,000 images.");

        public static Error NewsLimitExceeded(Guid urlId) =>
            new("SITEMAP_NEWS_LIMIT_EXCEEDED", $"URL '{urlId}' cannot have more than one news entry.");
    }

    private readonly List<SitemapUrl> _urls = [];
    private readonly List<SitemapId> _childSitemapIds = [];

    public Url Location { get; private set; } = null!;
    public DateTime? LastModified { get; private set; }
    public bool IsMapped { get; private set; }
    public SitemapId? ParentSitemapId { get; private set; }
    public UrlLimit MaxUrls { get; private set; } = null!;

    public IReadOnlyList<SitemapUrl> Urls => _urls.AsReadOnly();
    public IReadOnlyList<SitemapId> ChildSitemapIds => _childSitemapIds.AsReadOnly();
    public bool IsSitemapIndex => _childSitemapIds.Count > 0;

    private Sitemap() { }

    public static Sitemap Create(string location, UrlLimit maxUrls, SitemapId? parentSitemapId = null)
    {
        Guard.AgainstNull(maxUrls);

        var sitemap = new Sitemap
        {
            Id = Guid.NewGuid(),
            Location = new Url(location),
            MaxUrls = maxUrls,
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
        if (MaxUrls.IsExceededBy(_urls.Count))
            throw new DomainLayerException(Errors.UrlLimitExceeded(MaxUrls.Value));

        var loc = new Url(location);

        if (!string.Equals(loc.Host, Location.Host, StringComparison.OrdinalIgnoreCase))
            throw new DomainLayerException(Errors.UrlHostMismatch(loc.Host, Location.Host));

        if (_urls.Any(u => u.Location == loc))
            throw new DomainLayerException(Errors.DuplicateUrl(location));

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
            ?? throw new DomainLayerException(Errors.UrlNotFound(urlId));

        _urls.Remove(url);
        SetUpdated();

        AddDomainEvent(
            SitemapUrlRemovedEvent.Create(Id, urlId));
    }

    public void AddChildSitemap(SitemapId childSitemapId)
    {
        Guard.AgainstNull(childSitemapId);

        if (childSitemapId.Value == Id)
            throw new DomainLayerException(Errors.SelfReferencingChild);

        if (_childSitemapIds.Contains(childSitemapId))
            throw new DomainLayerException(Errors.DuplicateChild(childSitemapId));

        _childSitemapIds.Add(childSitemapId);
        SetUpdated();

        AddDomainEvent(
            SitemapChildAddedEvent.Create(Id, childSitemapId.Value));
    }

    public void RemoveChildSitemap(SitemapId childSitemapId)
    {
        Guard.AgainstNull(childSitemapId);

        if (!_childSitemapIds.Remove(childSitemapId))
            throw new DomainLayerException(Errors.ChildNotFound(childSitemapId));

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

    public void RecordCrawlAttempt(Guid urlId)
    {
        var url = FindUrl(urlId);
        url.RecordCrawlAttempt();
        SetUpdated();
    }

    public void RecordCrawlSuccess(Guid urlId)
    {
        var url = FindUrl(urlId);
        url.RecordCrawlSuccess();
        SetUpdated();
    }

    public IReadOnlyList<SitemapUrl> GetUrlsDue(DateTime asOf)
        => _urls.Where(u => u.IsDue(asOf)).ToList().AsReadOnly();

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
           ?? throw new DomainLayerException(Errors.UrlNotFound(urlId));
}
