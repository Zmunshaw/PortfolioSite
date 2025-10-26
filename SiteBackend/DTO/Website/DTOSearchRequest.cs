using Pgvector;

namespace SiteBackend.DTO.Website;

public class DTOSearchRequest
{
    public DTOSearchRequest()
    {
    }

    public DTOSearchRequest(string query)
    {
        SearchQuery = query;
    }

    public string SearchQuery { get; set; }

    public Vector? DenseVector { get; set; }
    public SparseVector? SparseVector { get; set; }

    public List<DTOSearchResult>? SearchResults { get; set; }
}