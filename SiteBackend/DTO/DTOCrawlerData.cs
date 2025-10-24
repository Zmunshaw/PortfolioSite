using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.DTO;

public class DTOCrawlerData
{
    public DTOCrawlerData () {}
    
    public DTOCrawlerData(Page page)
    {
        PageID = page.PageID;
        PageUrl = page.Url.Location;
        Title = page.Content.Title;
        Text = page.Content.Text;
        CrawledAt = page.LastCrawled;
    }
    
    public int PageID { get; set; }
    
    public string PageUrl { get; set; }
    public string? Title { get; set; }
    public string? Text { get; set; }
    
    public DateTime? CrawledAt { get; set; }
    
    public Page ConvertToPage()
    {
        var convertedPage = new Page
        {
            PageID = PageID,
            LastCrawled = CrawledAt,
            Url = new Url(PageUrl),
        };
        convertedPage.Content = new Content(convertedPage, Title, Text);
        convertedPage.Url.Page = convertedPage.Content.Page;
        convertedPage.Content.ContentID = convertedPage.Content.ContentID;
        return convertedPage;
    }
}