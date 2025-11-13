using Pgvector;

namespace SiteBackend.DTO.Website;

public class DTOSearchRequest
{
    // Search Weights
    public readonly double SparseWeight = 0.4;
    public readonly double DenseWeight = 0.3;
    public readonly double KeywordWeight = 0.4;
    
    public DTOSearchRequest()
    {
    }

    public DTOSearchRequest(string query, int currentPage = 1, int resultsPerPage = 40,
        string? siteRestriction = null, double? maxDistance = null, double sparseWeight = 0.3,
        double denseWeight = 0.3, double keywordWeight = 0.4)
    {
        SearchQuery = query;
        CurrentPage = currentPage;
        PageSize = resultsPerPage;
        SiteRestriction = siteRestriction;
        MaxDistance = maxDistance;

        SparseWeight = sparseWeight;
        DenseWeight = denseWeight;
        KeywordWeight = keywordWeight;
    }

    // Meta
    public int CurrentPage { get; set; }
    public int PageSize { get; set; } = 25;

    // Query Data
    public string SearchQuery { get; set; }
    public string? SiteRestriction { get; set; }
    public double? MaxDistance { get; set; }

    // Vector Data
    public Vector? DenseVector { get; set; }
    public SparseVector? SparseVector { get; set; }
}