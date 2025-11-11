using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.DTO.Website;

public class DTOSearchResult
{
    public DTOSearchResult()
    {
    }

    public DTOSearchResult(Page page, double resultScore, double denseDistance, double sparseDistance,
        bool keywordMatch)
    {
        ResultPage = page;
        ResultScore = resultScore;
        DenseDistance = denseDistance;
        SparseDistance = sparseDistance;
        KeywordMatch = keywordMatch;
    }

    public Page ResultPage { get; set; }

    public double ResultScore { get; set; }
    public double DenseDistance { get; set; }
    public double SparseDistance { get; set; }
    public bool KeywordMatch { get; set; }
}