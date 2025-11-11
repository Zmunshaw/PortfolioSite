using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.DTO;

public class DTOCrawlRequest
{
    public DTOCrawlRequest(Page page)
    {
        PageID = page.PageID;
        Url = page.Url.Location;
    }

    public int PageID { get; set; }
    public string Url { get; set; }
}