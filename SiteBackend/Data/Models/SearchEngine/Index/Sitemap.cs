using System.ComponentModel.DataAnnotations;

namespace SiteBackend.Models.SearchEngine.Index;

public enum ChangeFrequency
{
    Always,
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Yearly,
    Unknown
}

public enum MediaType
{
    Image,
    Video,
    News,
}

public class Sitemap
{
    public Sitemap()
    {
    }

    public Sitemap(Website website, string? location = null, List<Url>? urlSet = null)
    {
        WebsiteID = website.WebsiteID;
        Website = website;
        Location = location ?? website.Host;
        UrlSet = urlSet ?? [];
    }

    [Key] public int SitemapID { get; set; }

    // Foreign key to Website (one-to-one)
    public int WebsiteID { get; set; }
    public Website Website { get; set; }

    [Required] public string Location { get; set; }

    public DateTime? LastModified { get; set; }

    // Self-referencing foreign key for parent sitemap
    public int? ParentSitemapId { get; set; }
    public Sitemap? ParentSitemap { get; set; }

    // Child sitemaps
    public List<Sitemap> SitemapIndex { get; set; } = new();

    // URLs in this sitemap
    public List<Url> UrlSet { get; set; } = new();

    public bool IsMapped { get; set; } = false;
}

public class Url
{
    public Url()
    {
    }

    public Url(string location, Sitemap? sitemap = null, Page? page = null)
    {
        Location = location;
        if (sitemap != null)
            SitemapID = sitemap.SitemapID;
        if (page != null)
            PageID = page.PageID;
    }

    [Key] public int UrlID { get; set; }

    // Foreign key to Sitemap (many-to-one)
    public int? SitemapID { get; set; }
    public Sitemap? Sitemap { get; set; }

    // Foreign key to Page (one-to-one)
    public int PageID { get; set; }
    public Page Page { get; set; }

    [Required] public string Location { get; set; }

    public DateTime? LastModified { get; set; }

    public ChangeFrequency? ChangeFrequency { get; set; }

    [Range(0.0, 1.0)] public float Priority { get; set; } = 0.5f;

    // One-to-many with MediaEntry
    public List<MediaEntry> Media { get; set; } = new();
}

// Polymorphic table with discriminator
public abstract class MediaEntry
{
    [Key] public int MediaEntryID { get; set; }

    [Required] public string Location { get; set; } = default!;

    public MediaType Type { get; set; }

    // Foreign key to Url
    public int UrlID { get; set; }
    public Url Url { get; set; }
}

public class ImageEntry : MediaEntry
{
    // Inherits Location from MediaEntry - don't redefine it
}

public class VideoEntry : MediaEntry
{
    public string? ThumbnailLocation { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? ContentLocation { get; set; }

    public string? PlayerLocation { get; set; }

    public TimeSpan? Duration { get; set; }

    [Range(0.0, 5.0)] public float Rating { get; set; } = 2.5f;

    public int? ViewCount { get; set; }

    public DateTime? PublicationDate { get; set; }

    public string? Restrictions { get; set; }

    public string? Platform { get; set; }

    public string? RequiresSubscription { get; set; }

    public string? Tag { get; set; }
}

public class NewsEntry : MediaEntry
{
    public string? Publication { get; set; }

    public DateTime? PublicationDate { get; set; }

    public string? Language { get; set; }

    public string? Title { get; set; }
}