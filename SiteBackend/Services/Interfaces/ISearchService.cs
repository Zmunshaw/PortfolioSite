using SiteBackend.DTO.Website;

namespace SiteBackend.Services.Controllers;

public interface ISearchService
{
    Task<DTOSearchRequest> GetResults(string query);
}