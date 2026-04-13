using Portfolio.Domain.Aggregates.Sitemap;
using Portfolio.Domain.Aggregates.Sitemap.Enums;
using Portfolio.Domain.Aggregates.Sitemap.Events;
using Portfolio.Domain.Aggregates.Sitemap.ValueObjects;
using Portfolio.Domain.Errors;

namespace Unit.Domain.Aggregates.Sitemap;

public class SitemapTests
{
    private const string ValidLocation = "https://example.com/sitemap.xml";

    private static Portfolio.Domain.Aggregates.Sitemap.Sitemap CreateSitemap(
        string location = ValidLocation,
        int maxUrls = 50000)
        => Portfolio.Domain.Aggregates.Sitemap.Sitemap.Create(location, new UrlLimit(maxUrls));

    // --- Create ---

    [Fact]
    public void Create_ValidInput_SetsPropertiesAndRaisesEvent()
    {
        var sitemap = CreateSitemap();

        Assert.NotEqual(Guid.Empty, sitemap.Id);
        Assert.Equal("https://example.com/sitemap.xml", sitemap.Location.Value);
        Assert.Equal(50000, sitemap.MaxUrls.Value);
        Assert.False(sitemap.IsMapped);
        Assert.Null(sitemap.LastModified);
        Assert.Null(sitemap.ParentSitemapId);
        Assert.Empty(sitemap.Urls);
        Assert.Empty(sitemap.ChildSitemapIds);

        var evt = Assert.Single(sitemap.DomainEvents);
        Assert.IsType<SitemapCreatedEvent>(evt);
    }

    [Fact]
    public void Create_WithParentSitemapId_SetsParent()
    {
        var parentId = SitemapId.New();
        var sitemap = Portfolio.Domain.Aggregates.Sitemap.Sitemap.Create(
            ValidLocation, new UrlLimit(100), parentId);

        Assert.Equal(parentId, sitemap.ParentSitemapId);
    }

    [Fact]
    public void Create_NullMaxUrls_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Portfolio.Domain.Aggregates.Sitemap.Sitemap.Create(ValidLocation, null!));
    }

    // --- AddUrl ---

    [Fact]
    public void AddUrl_ValidUrl_AddsAndRaisesEvent()
    {
        var sitemap = CreateSitemap();

        var url = sitemap.AddUrl("https://example.com/page1");

        Assert.Single(sitemap.Urls);
        Assert.Equal("https://example.com/page1", url.Location.Value);
        Assert.Contains(sitemap.DomainEvents, e => e is SitemapUrlAddedEvent);
    }

    [Fact]
    public void AddUrl_WithOptionalParams_SetsProperties()
    {
        var sitemap = CreateSitemap();
        var lastMod = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var url = sitemap.AddUrl(
            "https://example.com/page1",
            lastModified: lastMod,
            changeFrequency: ChangeFrequency.Daily,
            priority: 0.8f);

        Assert.Equal(lastMod, url.LastModified);
        Assert.Equal(ChangeFrequency.Daily, url.ChangeFrequency);
        Assert.Equal(0.8f, url.Priority.Value);
    }

    [Fact]
    public void AddUrl_ExceedsLimit_ThrowsDomainException()
    {
        var sitemap = CreateSitemap(maxUrls: 1);
        sitemap.AddUrl("https://example.com/page1");

        var ex = Assert.Throws<DomainLayerException>(() =>
            sitemap.AddUrl("https://example.com/page2"));

        Assert.Contains("SITEMAP_URL_LIMIT_EXCEEDED", ex.Error.Code);
    }

    [Fact]
    public void AddUrl_DuplicateUrl_ThrowsDomainException()
    {
        var sitemap = CreateSitemap();
        sitemap.AddUrl("https://example.com/page1");

        var ex = Assert.Throws<DomainLayerException>(() =>
            sitemap.AddUrl("https://example.com/page1"));

        Assert.Contains("SITEMAP_DUPLICATE_URL", ex.Error.Code);
    }

    [Fact]
    public void AddUrl_HostMismatch_ThrowsDomainException()
    {
        var sitemap = CreateSitemap();

        var ex = Assert.Throws<DomainLayerException>(() =>
            sitemap.AddUrl("https://other-domain.com/page1"));

        Assert.Contains("SITEMAP_URL_HOST_MISMATCH", ex.Error.Code);
    }

    // --- RemoveUrl ---

    [Fact]
    public void RemoveUrl_Existing_RemovesAndRaisesEvent()
    {
        var sitemap = CreateSitemap();
        var url = sitemap.AddUrl("https://example.com/page1");

        sitemap.RemoveUrl(url.Id);

        Assert.Empty(sitemap.Urls);
        Assert.Contains(sitemap.DomainEvents, e => e is SitemapUrlRemovedEvent);
    }

    [Fact]
    public void RemoveUrl_NotFound_ThrowsDomainException()
    {
        var sitemap = CreateSitemap();

        var ex = Assert.Throws<DomainLayerException>(() =>
            sitemap.RemoveUrl(Guid.NewGuid()));

        Assert.Contains("SITEMAP_URL_NOT_FOUND", ex.Error.Code);
    }

    // --- Child Sitemaps ---

    [Fact]
    public void AddChildSitemap_Valid_AddsAndRaisesEvent()
    {
        var sitemap = CreateSitemap();
        var childId = SitemapId.New();

        sitemap.AddChildSitemap(childId);

        Assert.Single(sitemap.ChildSitemapIds);
        Assert.Contains(sitemap.DomainEvents, e => e is SitemapChildAddedEvent);
    }

    [Fact]
    public void AddChildSitemap_SelfReference_ThrowsDomainException()
    {
        var sitemap = CreateSitemap();
        var selfId = new SitemapId(sitemap.Id);

        var ex = Assert.Throws<DomainLayerException>(() =>
            sitemap.AddChildSitemap(selfId));

        Assert.Contains("SITEMAP_SELF_REFERENCING_CHILD", ex.Error.Code);
    }

    [Fact]
    public void AddChildSitemap_Duplicate_ThrowsDomainException()
    {
        var sitemap = CreateSitemap();
        var childId = SitemapId.New();
        sitemap.AddChildSitemap(childId);

        var ex = Assert.Throws<DomainLayerException>(() =>
            sitemap.AddChildSitemap(childId));

        Assert.Contains("SITEMAP_DUPLICATE_CHILD", ex.Error.Code);
    }

    [Fact]
    public void RemoveChildSitemap_Existing_Removes()
    {
        var sitemap = CreateSitemap();
        var childId = SitemapId.New();
        sitemap.AddChildSitemap(childId);

        sitemap.RemoveChildSitemap(childId);

        Assert.Empty(sitemap.ChildSitemapIds);
    }

    [Fact]
    public void RemoveChildSitemap_NotFound_ThrowsDomainException()
    {
        var sitemap = CreateSitemap();

        var ex = Assert.Throws<DomainLayerException>(() =>
            sitemap.RemoveChildSitemap(SitemapId.New()));

        Assert.Contains("SITEMAP_CHILD_NOT_FOUND", ex.Error.Code);
    }

    [Fact]
    public void IsSitemapIndex_WithChildren_ReturnsTrue()
    {
        var sitemap = CreateSitemap();
        sitemap.AddChildSitemap(SitemapId.New());

        Assert.True(sitemap.IsSitemapIndex);
    }

    [Fact]
    public void IsSitemapIndex_WithoutChildren_ReturnsFalse()
    {
        var sitemap = CreateSitemap();

        Assert.False(sitemap.IsSitemapIndex);
    }

    // --- Media ---

    [Fact]
    public void AddImageToUrl_Valid_AddsImage()
    {
        var sitemap = CreateSitemap();
        var url = sitemap.AddUrl("https://example.com/page1");

        sitemap.AddImageToUrl(url.Id, "https://example.com/image.png");

        Assert.Single(sitemap.Urls[0].Images);
    }

    [Fact]
    public void AddImageToUrl_UrlNotFound_ThrowsDomainException()
    {
        var sitemap = CreateSitemap();

        Assert.Throws<DomainLayerException>(() =>
            sitemap.AddImageToUrl(Guid.NewGuid(), "https://example.com/image.png"));
    }

    [Fact]
    public void AddVideoToUrl_Valid_AddsVideo()
    {
        var sitemap = CreateSitemap();
        var url = sitemap.AddUrl("https://example.com/page1");
        var video = new VideoMedia(
            "https://example.com/thumb.jpg",
            "Test Video",
            "A test video description",
            contentLocation: "https://example.com/video.mp4");

        sitemap.AddVideoToUrl(url.Id, video);

        Assert.Single(sitemap.Urls[0].Videos);
    }

    [Fact]
    public void AddNewsToUrl_Valid_AddsNews()
    {
        var sitemap = CreateSitemap();
        var url = sitemap.AddUrl("https://example.com/page1");
        var news = new NewsMedia(
            new Publication("Test Pub", "en"),
            DateTime.UtcNow,
            "Breaking News");

        sitemap.AddNewsToUrl(url.Id, news);

        Assert.Single(sitemap.Urls[0].News);
    }

    [Fact]
    public void AddNewsToUrl_ExceedsLimit_ThrowsDomainException()
    {
        var sitemap = CreateSitemap();
        var url = sitemap.AddUrl("https://example.com/page1");
        var news1 = new NewsMedia(
            new Publication("Pub", "en"), DateTime.UtcNow, "News 1");
        var news2 = new NewsMedia(
            new Publication("Pub", "en"), DateTime.UtcNow, "News 2");

        sitemap.AddNewsToUrl(url.Id, news1);

        Assert.Throws<DomainLayerException>(() =>
            sitemap.AddNewsToUrl(url.Id, news2));
    }

    [Fact]
    public void AddAlternateLinkToUrl_Valid_AddsLink()
    {
        var sitemap = CreateSitemap();
        var url = sitemap.AddUrl("https://example.com/page1");

        sitemap.AddAlternateLinkToUrl(url.Id, "en-us", "https://example.com/en/page1");

        Assert.Single(sitemap.Urls[0].AlternateLinks);
    }

    // --- Crawl Tracking ---

    [Fact]
    public void RecordCrawlAttempt_UpdatesCrawlState()
    {
        var sitemap = CreateSitemap();
        var url = sitemap.AddUrl("https://example.com/page1");

        sitemap.RecordCrawlAttempt(url.Id);

        Assert.Equal(1, sitemap.Urls[0].CrawlState.Attempts);
        Assert.NotNull(sitemap.Urls[0].CrawlState.LastAttempt);
    }

    [Fact]
    public void RecordCrawlSuccess_UpdatesCrawlState()
    {
        var sitemap = CreateSitemap();
        var url = sitemap.AddUrl("https://example.com/page1");

        sitemap.RecordCrawlSuccess(url.Id);

        Assert.Equal(1, sitemap.Urls[0].CrawlState.Attempts);
        Assert.NotNull(sitemap.Urls[0].CrawlState.LastSuccess);
    }

    // --- GetUrlsDue ---

    [Fact]
    public void GetUrlsDue_ReturnsOnlyDueUrls()
    {
        var sitemap = CreateSitemap();
        sitemap.AddUrl("https://example.com/page1", changeFrequency: ChangeFrequency.Daily);
        sitemap.AddUrl("https://example.com/page2", changeFrequency: ChangeFrequency.Never);

        // Both are due initially (never crawled)
        var dueUrls = sitemap.GetUrlsDue(DateTime.UtcNow);
        Assert.Equal(2, dueUrls.Count);

        // After crawling page2, it should no longer be due (Never frequency)
        sitemap.RecordCrawlSuccess(sitemap.Urls[1].Id);
        dueUrls = sitemap.GetUrlsDue(DateTime.UtcNow);
        Assert.Single(dueUrls);
        Assert.Equal("https://example.com/page1", dueUrls[0].Location.Value);
    }

    // --- State Management ---

    [Fact]
    public void MarkAsMapped_SetsFlag()
    {
        var sitemap = CreateSitemap();

        sitemap.MarkAsMapped();

        Assert.True(sitemap.IsMapped);
        Assert.NotNull(sitemap.UpdatedAt);
    }

    [Fact]
    public void UpdateLastModified_SetsDate()
    {
        var sitemap = CreateSitemap();
        var date = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        sitemap.UpdateLastModified(date);

        Assert.Equal(date, sitemap.LastModified);
        Assert.NotNull(sitemap.UpdatedAt);
    }
}
