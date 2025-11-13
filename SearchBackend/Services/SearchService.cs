using Microsoft.EntityFrameworkCore;
using Pgvector;
using SearchBackend.Services.Exceptions;
using SiteBackend.Database;
using SiteBackend.DTO.Website;
using SiteBackend.Repositories.SearchEngine.Interfaces;
using SiteBackend.Services.Controllers;

namespace SiteBackend.Services;

public class SearchService : ISearchService
{
    private readonly IAIService _aiService;
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    private readonly ILogger<SearchService> _logger;
    private readonly ISearchRepo _searchRepo;

    public SearchService(ILogger<SearchService> logger, IAIService aiService, ISearchRepo contentRepo)
    {
        _logger = logger;
        _aiService = aiService;
        _searchRepo = contentRepo;
    }
    
    public async Task<IEnumerable<DTOSearchResult>> GetResults(DTOSearchRequest searchRequest)
    {
        _logger.LogDebug("Getting results from search request...");
        SearchQueryException.ThrowIfInvalid(searchRequest.SearchQuery);
        IEnumerable<DTOSearchResult> results;
        
        try
        {
            (searchRequest.DenseVector, searchRequest.SparseVector) = 
                await VectorizeQuery(searchRequest.SearchQuery);
            return await _searchRepo.GetSearchResults(searchRequest);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Search operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: {Query}", searchRequest.SearchQuery);
            throw;
        }
    }

    private async Task<(Vector, SparseVector)> VectorizeQuery(string query)
    {
        _logger.LogDebug("Vectorizing query: {query}...", query);
        var denseVector = await _aiService.GetDenseSearchVectorAsync(query);
        var sparseVector = await _aiService.GetSparseSearchVectorAsync(query);
        return (denseVector, sparseVector);
    }
}