using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Aggregates.Shared;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class VideoMedia : ValueObject
{
    public Url ThumbnailLocation { get; }
    public string Title { get; }
    public string Description { get; }
    public Url? ContentLocation { get; }
    public Url? PlayerLocation { get; }
    public VideoDuration? Duration { get; }
    public DateTime? ExpirationDate { get; }
    public VideoRating? Rating { get; }
    public int? ViewCount { get; }
    public DateTime? PublicationDate { get; }
    public bool? FamilyFriendly { get; }
    public bool? RequiresSubscription { get; }
    public string? Uploader { get; }
    public bool? Live { get; }
    public TagSet Tags { get; }
    public AccessPolicy? CountryRestriction { get; }
    public AccessPolicy? PlatformRestriction { get; }

    public VideoMedia(
        string thumbnailLocation,
        string title,
        string description,
        string? contentLocation = null,
        string? playerLocation = null,
        VideoDuration? duration = null,
        DateTime? expirationDate = null,
        VideoRating? rating = null,
        int? viewCount = null,
        DateTime? publicationDate = null,
        bool? familyFriendly = null,
        bool? requiresSubscription = null,
        string? uploader = null,
        bool? live = null,
        TagSet? tags = null,
        AccessPolicy? countryRestriction = null,
        AccessPolicy? platformRestriction = null)
    {
        ThumbnailLocation = new Url(thumbnailLocation);
        Title = Guard.AgainstNullOrWhiteSpace(title);
        Description = Guard.AgainstNullOrWhiteSpace(description);

        if (description.Length > 2048)
            throw new ArgumentException("Video description cannot exceed 2048 characters.", nameof(description));

        if (contentLocation is null && playerLocation is null)
            throw new ArgumentException("At least one of contentLocation or playerLocation must be provided.");

        ContentLocation = contentLocation is not null ? new Url(contentLocation) : null;
        PlayerLocation = playerLocation is not null ? new Url(playerLocation) : null;

        if (uploader is not null && uploader.Length > 255)
            throw new ArgumentException("Uploader name cannot exceed 255 characters.", nameof(uploader));

        Duration = duration;
        ExpirationDate = expirationDate;
        Rating = rating;
        ViewCount = viewCount;
        PublicationDate = publicationDate;
        FamilyFriendly = familyFriendly;
        RequiresSubscription = requiresSubscription;
        Uploader = uploader;
        Live = live;
        Tags = tags ?? TagSet.Empty;
        CountryRestriction = countryRestriction;
        PlatformRestriction = platformRestriction;
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
