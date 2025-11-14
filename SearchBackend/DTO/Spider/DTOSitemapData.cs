namespace SiteBackend.DTO;

public class DTOSitemapData
{
    public int SitemapID { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public List<DTOSitemapData> SitemapIndex { get; set; } = new();
    public List<DTOUrlData> UrlSet { get; set; } = new();
    public bool IsMapped { get; set; }
}