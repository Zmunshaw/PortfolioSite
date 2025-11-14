using Microsoft.EntityFrameworkCore;
using Pgvector;
using SearchBackend.Repositories.SearchEngine.Interfaces;
using SearchBackend.Services.Exceptions;
using SiteBackend.Database;
using SiteBackend.DTO.Website;
using SiteBackend.Services;
using SiteBackend.Services.Controllers;

namespace SearchBackend.Services;

public class SearchService(ILogger<SearchService> logger, IAIService aiService, ISearchRepo contentRepo,
    IDbContextFactory<SearchEngineCtx> ctxFactory) : ISearchService
{
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory = ctxFactory;

    public async Task<IEnumerable<DTOSearchResult>> GetResults(DTOSearchRequest searchRequest)
    {
        logger.LogDebug("Getting results from search request...");
        SearchQueryException.ThrowIfInvalid(searchRequest.SearchQuery);
        IEnumerable<DTOSearchResult> results;
        
        try
        {
            (searchRequest.DenseVector, searchRequest.SparseVector) = 
                await VectorizeQuery(searchRequest.SearchQuery);
            results = await contentRepo.GetSearchResults(searchRequest);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Search operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search failed for query: {Query}", searchRequest.SearchQuery);
            throw;
        }

        var dtoSearchResults = results as DTOSearchResult[] ?? results.ToArray();
        if (!dtoSearchResults.Any())
        {
            logger.LogWarning("No results found for query: {Query}", searchRequest.SearchQuery);    
        }
        
        return dtoSearchResults;
    }

    private async Task<(Vector, SparseVector)> VectorizeQuery(string query)
    {
        logger.LogDebug("Vectorizing query: {query}...", query);
        var denseVector = await aiService.GetDenseSearchVectorAsync(query);
        var sparseVector = await aiService.GetSparseSearchVectorAsync(query);
        return (denseVector, sparseVector);
    }
}