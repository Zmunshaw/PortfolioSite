namespace SiteBackend.DTO;

public class DTOMediaData
{
    public int MediaEntryID { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Image", "Video", "News"

    // Video fields
    public string? ThumbnailLocation { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ContentLocation { get; set; }
    public string? PlayerLocation { get; set; }
    public string? Duration { get; set; }
    public float? Rating { get; set; }
    public int? ViewCount { get; set; }
    public DateTime? PublicationDate { get; set; }
    public string? Restrictions { get; set; }
    public string? Platform { get; set; }
    public string? RequiresSubscription { get; set; }
    public string? Tag { get; set; }

    // News fields
    public string? Publication { get; set; }
    public string? Language { get; set; }
}
