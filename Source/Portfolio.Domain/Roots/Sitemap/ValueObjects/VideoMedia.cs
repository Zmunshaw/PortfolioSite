using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Roots.Sitemap.ValueObjects;

public sealed class VideoMedia : ValueObject
{
    public SitemapLocation ThumbnailLocation { get; }
    public string Title { get; }
    public string Description { get; }
    public SitemapLocation? ContentLocation { get; }
    public SitemapLocation? PlayerLocation { get; }
    public int? Duration { get; }
    public DateTime? ExpirationDate { get; }
    public float? Rating { get; }
    public int? ViewCount { get; }
    public DateTime? PublicationDate { get; }
    public bool? FamilyFriendly { get; }
    public bool? RequiresSubscription { get; }
    public string? Uploader { get; }
    public bool? Live { get; }
    public IReadOnlyList<string> Tags { get; }
    public VideoRestriction? Restriction { get; }
    public VideoPlatform? Platform { get; }

    public VideoMedia(
        string thumbnailLocation,
        string title,
        string description,
        string? contentLocation = null,
        string? playerLocation = null,
        int? duration = null,
        DateTime? expirationDate = null,
        float? rating = null,
        int? viewCount = null,
        DateTime? publicationDate = null,
        bool? familyFriendly = null,
        bool? requiresSubscription = null,
        string? uploader = null,
        bool? live = null,
        IEnumerable<string>? tags = null,
        VideoRestriction? restriction = null,
        VideoPlatform? platform = null)
    {
        ThumbnailLocation = new SitemapLocation(thumbnailLocation);
        Title = Guard.AgainstNullOrWhiteSpace(title);
        Description = Guard.AgainstNullOrWhiteSpace(description);

        if (description.Length > 2048)
            throw new ArgumentException("Video description cannot exceed 2048 characters.", nameof(description));

        if (contentLocation is null && playerLocation is null)
            throw new ArgumentException("At least one of contentLocation or playerLocation must be provided.");

        ContentLocation = contentLocation is not null ? new SitemapLocation(contentLocation) : null;
        PlayerLocation = playerLocation is not null ? new SitemapLocation(playerLocation) : null;

        if (duration.HasValue)
            Guard.AgainstOutOfRange(duration.Value, 1, 28800);
        Duration = duration;

        if (rating.HasValue)
            Guard.AgainstOutOfRange(rating.Value, 0.0f, 5.0f);
        Rating = rating;

        if (uploader is not null && uploader.Length > 255)
            throw new ArgumentException("Uploader name cannot exceed 255 characters.", nameof(uploader));

        var tagList = tags?.ToList() ?? [];
        if (tagList.Count > 32)
            throw new ArgumentException("Video cannot have more than 32 tags.", nameof(tags));
        Tags = tagList.AsReadOnly();

        ExpirationDate = expirationDate;
        ViewCount = viewCount;
        PublicationDate = publicationDate;
        FamilyFriendly = familyFriendly;
        RequiresSubscription = requiresSubscription;
        Uploader = uploader;
        Live = live;
        Restriction = restriction;
        Platform = platform;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ThumbnailLocation;
        yield return Title;
        yield return Description;
        yield return ContentLocation;
        yield return PlayerLocation;
    }
}
