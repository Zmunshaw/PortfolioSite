using SiteBackend.DTO.Website;

namespace SiteBackend.Services.Controllers;

public interface ISearchService
{
    Task<IEnumerable<DTOSearchResult>> GetResults(DTOSearchRequest request);
}