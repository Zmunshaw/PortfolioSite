using System.Text.Json.Serialization;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.DTO.Website;

public class DTOSearchResult
{
    public DTOSearchResult()
    {
    }

    public DTOSearchResult(Page page, string pgTitle, string pgDesc, string pgUrl, double resultScore,
        double denseDistance, double sparseDistance, bool keywordMatch)
    {
        ResultPage = page;
        ResultScore = resultScore;
        DenseDistance = denseDistance;
        SparseDistance = sparseDistance;
        KeywordMatch = keywordMatch;

        ResultTitle = pgTitle;
        ResultDescription = pgDesc;
        ResultUrl = pgUrl;
    }

    [JsonIgnore] public Page ResultPage { get; set; }

    public string ResultTitle { get; set; }
    public string ResultUrl { get; set; }
    public string ResultDescription { get; set; }
    public double ResultScore { get; set; }
    public double DenseDistance { get; set; }
    public double SparseDistance { get; set; }
    public bool KeywordMatch { get; set; }
}