namespace SiteBackend.DTO;

// DTO for scraper response
public class DTOScraperResult
{
    public string Url { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Error { get; set; }
}