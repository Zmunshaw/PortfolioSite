using Microsoft.EntityFrameworkCore;
using Pgvector;
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
        _logger.LogDebug("Getting results for {query}...", searchRequest.SearchQuery);
        (searchRequest.DenseVector, searchRequest.SparseVector) = await VectorizeQuery(searchRequest.SearchQuery);

        return await _searchRepo.GetSearchResults(searchRequest);
    }

    private async Task<(Vector, SparseVector)> VectorizeQuery(string query)
    {
        _logger.LogDebug("Vectorizing query: {query}...", query);
        var denseTask = _aiService.GetDenseSearchVectorAsync(query);
        var sparseTask = _aiService.GetSparseSearchVectorAsync(query);

        await Task.WhenAll(denseTask, sparseTask);

        return (denseTask.Result, sparseTask.Result);
    }

    #region Words

    #endregion
}