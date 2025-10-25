using Pgvector;
using SiteBackend.Models.SearchEngine.Index;

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
    public Vector? QueryVector { get; set; }
    public List<TextEmbedding> ProximalEmbeddings { get; set; }

    public List<DTOSearchResult>? SearchResults { get; set; }
}