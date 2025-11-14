namespace SiteBackend.DTO;

public class DTOSitemapRequest
{
    public int WebsiteID { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public int BatchSize { get; set; } = 50;
    public int MaxConcurrent { get; set; } = 5;
}