using Portfolio.Domain.Aggregates.Sitemap.Enums;
using Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

namespace Unit.Domain.Aggregates.Sitemap.ValueObjects;

public class SitemapIdTests
{
    [Fact]
    public void SitemapId_ValidGuid_Creates()
    {
        var guid = Guid.NewGuid();
        var id = new SitemapId(guid);
        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void SitemapId_EmptyGuid_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SitemapId(Guid.Empty));
    }

    [Fact]
    public void SitemapId_New_GeneratesNonEmpty()
    {
        var id = SitemapId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void SitemapId_EqualValues_AreEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = new SitemapId(guid);
        var id2 = new SitemapId(guid);
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void SitemapId_DifferentValues_AreNotEqual()
    {
        var id1 = SitemapId.New();
        var id2 = SitemapId.New();
        Assert.NotEqual(id1, id2);
    }
}

public class UrlLimitTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(50000)]
    [InlineData(int.MaxValue)]
    public void UrlLimit_ValidValue_Creates(int value)
    {
        var limit = new UrlLimit(value);
        Assert.Equal(value, limit.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UrlLimit_ZeroOrNegative_Throws(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UrlLimit(value));
    }

    [Fact]
    public void IsExceededBy_AtLimit_ReturnsTrue()
    {
        var limit = new UrlLimit(5);
        Assert.True(limit.IsExceededBy(5));
    }

    [Fact]
    public void IsExceededBy_AboveLimit_ReturnsTrue()
    {
        var limit = new UrlLimit(5);
        Assert.True(limit.IsExceededBy(10));
    }

    [Fact]
    public void IsExceededBy_BelowLimit_ReturnsFalse()
    {
        var limit = new UrlLimit(5);
        Assert.False(limit.IsExceededBy(4));
    }

    [Fact]
    public void UrlLimit_EqualValues_AreEqual()
    {
        Assert.Equal(new UrlLimit(100), new UrlLimit(100));
    }
}

public class UrlPriorityTests
{
    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void UrlPriority_InRange_Creates(float value)
    {
        var priority = new UrlPriority(value);
        Assert.Equal(value, priority.Value);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void UrlPriority_OutOfRange_Throws(float value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UrlPriority(value));
    }

    [Fact]
    public void UrlPriority_Default_IsHalf()
    {
        Assert.Equal(0.5f, UrlPriority.Default.Value);
    }
}

public class CrawlStateTests
{
    [Fact]
    public void Initial_HasNullDatesZeroAttempts()
    {
        var state = CrawlState.Initial;

        Assert.Null(state.LastAttempt);
        Assert.Null(state.LastSuccess);
        Assert.Equal(0, state.Attempts);
    }

    [Fact]
    public void WithAttempt_IncrementsAndSetsLastAttempt()
    {
        var now = DateTime.UtcNow;
        var state = CrawlState.Initial.WithAttempt(now);

        Assert.Equal(now, state.LastAttempt);
        Assert.Null(state.LastSuccess);
        Assert.Equal(1, state.Attempts);
    }

    [Fact]
    public void WithSuccess_SetsLastSuccessAndAttempt()
    {
        var now = DateTime.UtcNow;
        var state = CrawlState.Initial.WithSuccess(now);

        Assert.Equal(now, state.LastAttempt);
        Assert.Equal(now, state.LastSuccess);
        Assert.Equal(1, state.Attempts);
    }

    [Fact]
    public void WithAttempt_MultipleChained_IncrementsCorrectly()
    {
        var t1 = DateTime.UtcNow;
        var t2 = t1.AddMinutes(5);

        var state = CrawlState.Initial
            .WithAttempt(t1)
            .WithAttempt(t2);

        Assert.Equal(t2, state.LastAttempt);
        Assert.Equal(2, state.Attempts);
    }

    [Fact]
    public void IsDue_NeverCrawled_ReturnsTrue()
    {
        var state = CrawlState.Initial;
        Assert.True(state.IsDue(DateTime.UtcNow, ChangeFrequency.Daily));
    }

    [Fact]
    public void IsDue_Always_ReturnsTrue()
    {
        var state = CrawlState.Initial.WithSuccess(DateTime.UtcNow);
        Assert.True(state.IsDue(DateTime.UtcNow, ChangeFrequency.Always));
    }

    [Fact]
    public void IsDue_Daily_NotYetDue_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        var state = CrawlState.Initial.WithSuccess(now);

        Assert.False(state.IsDue(now.AddHours(12), ChangeFrequency.Daily));
    }

    [Fact]
    public void IsDue_Daily_PastDue_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var state = CrawlState.Initial.WithSuccess(now);

        Assert.True(state.IsDue(now.AddDays(2), ChangeFrequency.Daily));
    }

    [Fact]
    public void IsDue_Never_ReturnsFalse()
    {
        var state = CrawlState.Initial.WithSuccess(DateTime.UtcNow);
        Assert.False(state.IsDue(DateTime.UtcNow.AddYears(100), ChangeFrequency.Never));
    }

    [Fact]
    public void IsDue_NullFrequency_DefaultsToDaily()
    {
        var now = DateTime.UtcNow;
        var state = CrawlState.Initial.WithSuccess(now);

        // Less than a day — not due
        Assert.False(state.IsDue(now.AddHours(12), null));
        // More than a day — due
        Assert.True(state.IsDue(now.AddDays(2), null));
    }
}

public class LanguageCodeTests
{
    [Fact]
    public void LanguageCode_Valid_NormalizesToLower()
    {
        var code = new LanguageCode("EN-US");
        Assert.Equal("en-us", code.Value);
    }

    [Fact]
    public void LanguageCode_XDefault_Works()
    {
        var code = LanguageCode.Default;
        Assert.Equal("x-default", code.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LanguageCode_NullOrWhitespace_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => new LanguageCode(value!));
    }
}

public class TagSetTests
{
    [Fact]
    public void TagSet_Valid_Creates()
    {
        var tags = new TagSet(["tag1", "tag2", "tag3"]);
        Assert.Equal(3, tags.Count);
    }

    [Fact]
    public void TagSet_ExceedsMax_Throws()
    {
        var tags = Enumerable.Range(1, 33).Select(i => $"tag{i}");
        Assert.Throws<ArgumentException>(() => new TagSet(tags));
    }

    [Fact]
    public void TagSet_At32_Succeeds()
    {
        var tags = Enumerable.Range(1, 32).Select(i => $"tag{i}");
        var tagSet = new TagSet(tags);
        Assert.Equal(32, tagSet.Count);
    }

    [Fact]
    public void TagSet_DeduplicatesCaseInsensitive()
    {
        var tags = new TagSet(["Tag", "TAG", "tag"]);
        Assert.Single(tags.Values);
    }

    [Fact]
    public void TagSet_TrimsWhitespace()
    {
        var tags = new TagSet(["  tag1  ", "tag2 "]);
        Assert.Equal("tag1", tags.Values[0]);
        Assert.Equal("tag2", tags.Values[1]);
    }

    [Fact]
    public void TagSet_FiltersOutBlanks()
    {
        var tags = new TagSet(["tag1", "", "  ", "tag2"]);
        Assert.Equal(2, tags.Count);
    }

    [Fact]
    public void TagSet_Empty_HasZeroCount()
    {
        Assert.Equal(0, TagSet.Empty.Count);
    }
}

public class VideoRatingTests
{
    [Theory]
    [InlineData(0.0f)]
    [InlineData(2.5f)]
    [InlineData(5.0f)]
    public void VideoRating_InRange_Creates(float value)
    {
        var rating = new VideoRating(value);
        Assert.Equal(value, rating.Value);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(5.1f)]
    public void VideoRating_OutOfRange_Throws(float value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VideoRating(value));
    }
}

public class VideoDurationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3600)]
    [InlineData(28800)]
    public void VideoDuration_InRange_Creates(int seconds)
    {
        var duration = new VideoDuration(seconds);
        Assert.Equal(seconds, duration.Seconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(28801)]
    public void VideoDuration_OutOfRange_Throws(int seconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VideoDuration(seconds));
    }

    [Fact]
    public void VideoDuration_ToTimeSpan_ReturnsCorrect()
    {
        var duration = new VideoDuration(3600);
        Assert.Equal(TimeSpan.FromHours(1), duration.ToTimeSpan());
    }
}

public class VideoPlatformTests
{
    [Fact]
    public void VideoPlatform_ValidAllowWeb_Creates()
    {
        var platform = new VideoPlatform("allow", ["web", "mobile"]);

        Assert.Equal("allow", platform.Relationship);
        Assert.Equal(2, platform.Platforms.Count);
    }

    [Fact]
    public void VideoPlatform_Deny_Creates()
    {
        var platform = new VideoPlatform("deny", ["tv"]);
        Assert.Equal("deny", platform.Relationship);
    }

    [Theory]
    [InlineData("block")]
    [InlineData("permit")]
    [InlineData("")]
    public void VideoPlatform_InvalidRelationship_Throws(string relationship)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new VideoPlatform(relationship, ["web"]));
    }

    [Fact]
    public void VideoPlatform_InvalidPlatformValue_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new VideoPlatform("allow", ["desktop"]));
    }

    [Fact]
    public void VideoPlatform_EmptyPlatforms_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new VideoPlatform("allow", Array.Empty<string>()));
    }
}

public class VideoRestrictionTests
{
    [Fact]
    public void VideoRestriction_ValidAllowCountries_Creates()
    {
        var restriction = new VideoRestriction("allow", ["US", "CA"]);

        Assert.Equal("allow", restriction.Relationship);
        Assert.Equal(2, restriction.Countries.Count);
    }

    [Theory]
    [InlineData("block")]
    [InlineData("permit")]
    public void VideoRestriction_InvalidRelationship_Throws(string relationship)
    {
        Assert.Throws<ArgumentException>(() =>
            new VideoRestriction(relationship, ["US"]));
    }

    [Fact]
    public void VideoRestriction_EmptyCountries_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new VideoRestriction("allow", Array.Empty<string>()));
    }
}

public class ImageMediaTests
{
    [Fact]
    public void ImageMedia_ValidUrl_Creates()
    {
        var image = new ImageMedia("https://example.com/image.png");
        Assert.Equal("https://example.com/image.png", image.Location.Value);
    }

    [Fact]
    public void ImageMedia_EqualLocations_AreEqual()
    {
        var a = new ImageMedia("https://example.com/image.png");
        var b = new ImageMedia("https://example.com/image.png");
        Assert.Equal(a, b);
    }
}

public class AlternateLinkTests
{
    [Fact]
    public void AlternateLink_Valid_Creates()
    {
        var link = new AlternateLink("en-us", "https://example.com/en/page");

        Assert.Equal("en-us", link.Hreflang.Value);
        Assert.Equal("https://example.com/en/page", link.Href.Value);
    }
}

public class PublicationTests
{
    [Fact]
    public void Publication_Valid_Creates()
    {
        var pub = new Publication("Example News", "en");

        Assert.Equal("Example News", pub.Name);
        Assert.Equal("en", pub.Language.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Publication_InvalidName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Publication(name!, "en"));
    }
}

public class NewsMediaTests
{
    [Fact]
    public void NewsMedia_Valid_Creates()
    {
        var pub = new Publication("Test Pub", "en");
        var date = DateTime.UtcNow;

        var news = new NewsMedia(pub, date, "Breaking News");

        Assert.Equal(pub, news.Publication);
        Assert.Equal(date, news.PublicationDate);
        Assert.Equal("Breaking News", news.Title);
    }

    [Fact]
    public void NewsMedia_NullPublication_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new NewsMedia(null!, DateTime.UtcNow, "Title"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NewsMedia_InvalidTitle_Throws(string? title)
    {
        var pub = new Publication("Pub", "en");
        Assert.Throws<ArgumentException>(() =>
            new NewsMedia(pub, DateTime.UtcNow, title!));
    }
}

public class VideoMediaTests
{
    [Fact]
    public void VideoMedia_WithContentLocation_Creates()
    {
        var video = new VideoMedia(
            "https://example.com/thumb.jpg",
            "Title",
            "Description",
            contentLocation: "https://example.com/video.mp4");

        Assert.Equal("Title", video.Title);
        Assert.Equal("Description", video.Description);
        Assert.NotNull(video.ContentLocation);
        Assert.Null(video.PlayerLocation);
    }

    [Fact]
    public void VideoMedia_WithPlayerLocation_Creates()
    {
        var video = new VideoMedia(
            "https://example.com/thumb.jpg",
            "Title",
            "Description",
            playerLocation: "https://example.com/player");

        Assert.NotNull(video.PlayerLocation);
        Assert.Null(video.ContentLocation);
    }

    [Fact]
    public void VideoMedia_NoContentOrPlayerLocation_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new VideoMedia(
                "https://example.com/thumb.jpg",
                "Title",
                "Description"));
    }

    [Fact]
    public void VideoMedia_DescriptionTooLong_Throws()
    {
        var longDesc = new string('x', 2049);
        Assert.Throws<ArgumentException>(() =>
            new VideoMedia(
                "https://example.com/thumb.jpg",
                "Title",
                longDesc,
                contentLocation: "https://example.com/video.mp4"));
    }

    [Fact]
    public void VideoMedia_DescriptionAt2048_Succeeds()
    {
        var desc = new string('x', 2048);
        var video = new VideoMedia(
            "https://example.com/thumb.jpg",
            "Title",
            desc,
            contentLocation: "https://example.com/video.mp4");

        Assert.Equal(2048, video.Description.Length);
    }

    [Fact]
    public void VideoMedia_UploaderNameTooLong_Throws()
    {
        var longName = new string('x', 256);
        Assert.Throws<ArgumentException>(() =>
            new VideoMedia(
                "https://example.com/thumb.jpg",
                "Title",
                "Description",
                contentLocation: "https://example.com/video.mp4",
                uploader: longName));
    }

    [Fact]
    public void VideoMedia_AllOptionalParams_Creates()
    {
        var video = new VideoMedia(
            "https://example.com/thumb.jpg",
            "Title",
            "Description",
            contentLocation: "https://example.com/video.mp4",
            playerLocation: "https://example.com/player",
            duration: new VideoDuration(120),
            expirationDate: DateTime.UtcNow.AddDays(30),
            rating: new VideoRating(4.5f),
            viewCount: 1000,
            publicationDate: DateTime.UtcNow,
            familyFriendly: true,
            requiresSubscription: false,
            uploader: "TestUser",
            uploaderInfo: "https://example.com/user",
            live: false,
            tags: new TagSet(["tag1", "tag2"]),
            restriction: new VideoRestriction("allow", ["US"]),
            platform: new VideoPlatform("allow", ["web"]));

        Assert.Equal(120, video.Duration!.Seconds);
        Assert.Equal(4.5f, video.Rating!.Value);
        Assert.Equal(1000, video.ViewCount);
        Assert.True(video.FamilyFriendly);
        Assert.False(video.RequiresSubscription);
        Assert.Equal("TestUser", video.Uploader);
        Assert.False(video.Live);
        Assert.Equal(2, video.Tags.Count);
    }
}
