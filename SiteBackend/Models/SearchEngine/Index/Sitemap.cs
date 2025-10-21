using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

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
    [Key]
    public int SitemapId { get; set; }

    [Required]
    public string Location { get; set; } = default!;
    
    public DateTime? LastModified { get; set; }
    
    public int? ParentSitemapId { get; set; } // FK
    
    public Sitemap? ParentSitemap { get; set; } // Navigation
    public List<Sitemap>? SitemapIndex { get; set; }
    

    public List<Url>? UrlSet { get; set; }
    
    public bool IsMapped {get; set;} = false;
}

public class Url
{
    [Key]
    public int UrlID { get; set; }

    [Required]
    public string Location { get; set; } = default!;

    public DateTime? LastModified { get; set; }

    public ChangeFrequency? ChangeFrequency { get; set; }

    [Range(0.0, 1.0)]
    public float Priority { get; set; } = 0.5f;

    public List<MediaEntry>? Media { get; set; }
}

public abstract class MediaEntry
{
    [Key]
    public int MediaEntryId { get; set; }
    
    public string Location { get; set; } = default!;
    
    public MediaType Type { get; set; }
}

public class ImageEntry : MediaEntry
{
    public string Location { get; set; } = default!;
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