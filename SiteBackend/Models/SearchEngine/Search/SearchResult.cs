namespace SiteBackend.Models.SearchEngine;

public struct SearchResult
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public required string Snippet { get; set; }
    public string? DisplayUrl { get; set; }
    public string? LastUpdated { get; set; }
}