using System.Text.Json.Serialization;

namespace SiteBackend.DTO;

// DTO for scraper response with comprehensive metadata
public class DTOScraperResult
{
    // Core fields (backward compatible)
    public string Url { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Content { get; set; }
    public List<string> Links { get; set; }
    public List<string> Images { get; set; }
    public string Error { get; set; }

    // Enhanced metadata
    public string? Keywords { get; set; }
    public string? Author { get; set; }
    public string? Published { get; set; }
    public string? Modified { get; set; }
    public string? Canonical { get; set; }
    public string? Language { get; set; }
    public int? WordCount { get; set; }

    // Structured data
    public DTOHeaders? Headers { get; set; }
    public Dictionary<string, string>? OpenGraph { get; set; }
    public Dictionary<string, string>? TwitterCard { get; set; }

    // Link analysis
    public List<string>? InternalLinks { get; set; }
    public List<string>? ExternalLinks { get; set; }
    public int? InternalLinkCount { get; set; }
    public int? ExternalLinkCount { get; set; }

    // Enhanced image data
    public List<DTOImageData>? ImageData { get; set; }
}

public class DTOHeaders
{
    public List<string>? H1 { get; set; }
    public List<string>? H2 { get; set; }
    public List<string>? H3 { get; set; }
}

public class DTOImageData
{
    public string Src { get; set; }
    public string? Alt { get; set; }
    public string? Title { get; set; }
}