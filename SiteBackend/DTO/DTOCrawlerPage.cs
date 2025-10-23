namespace SiteBackend.DTO;

public class DTOCrawlerPage
{
    public int WebsiteID { get; set; }
    
    public string Title { get; set; }
    public string Text { get; set; }
    public DateTime CrawlTime { get; set; }
}