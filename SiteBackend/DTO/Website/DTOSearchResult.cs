using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.DTO.Website;

public class DTOSearchResult
{
    public DTOSearchResult()
    {
    }

    public DTOSearchResult(Page page)
    {
        ResultPage = page;
    }

    public float Score { get; set; }
    public Page ResultPage { get; set; }
}