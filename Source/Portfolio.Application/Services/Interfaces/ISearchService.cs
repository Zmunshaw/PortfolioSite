namespace Portfolio.Application.Services.Interfaces;

public interface ISearchService
{
    Task<IEnumerable<DTOSearchResult>> GetResults(DTOSearchRequest request);
}