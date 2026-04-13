using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

namespace Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

public sealed class VideoMetadata : ValueObject
{
    public Url ThumbnailUrl { get; }
    public string Title { get; }
    public string? Description { get; }
    public Url? PlayerUrl { get; }
    public VideoDuration? Duration { get; }
    public int? ViewCount { get; }
    public DateTime? PublicationDate { get; }

    public VideoMetadata(
        string thumbnailUrl,
        string title,
        string? description = null,
        string? playerUrl = null,
        VideoDuration? duration = null,
        int? viewCount = null,
        DateTime? publicationDate = null)
    {
        ThumbnailUrl = new Url(thumbnailUrl);
        Title = Guard.AgainstNullOrWhiteSpace(title);

        if (description is not null && description.Length > 2048)
            throw new ArgumentException("Video description cannot exceed 2048 characters.", nameof(description));

        Description = description;
        PlayerUrl = playerUrl is not null ? new Url(playerUrl) : null;
        Duration = duration;
        ViewCount = viewCount;
        PublicationDate = publicationDate;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ThumbnailUrl;
        yield return Title;
        yield return PlayerUrl;
    }
}
