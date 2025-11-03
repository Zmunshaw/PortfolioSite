namespace SiteBackend.DTO;

// DTO for scraper response
public class DTOScraperResult
{
    public string Url { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Content { get; set; }
    public List<string> Links { get; set; }
    public List<string> Images { get; set; }
    public string Error { get; set; }
}