using Pgvector;
using SiteBackend.DTO.Website;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;
using SiteBackend.Services.Controllers;

namespace SiteBackend.Services;

public class SearchService : ISearchService
{
    private readonly IAIService _aiService;
    private readonly IContentRepo _contentRepo;
    private readonly ILogger<SearchService> _logger;

    public SearchService(ILogger<SearchService> logger, IAIService aiService, IContentRepo contentRepo)
    {
        _logger = logger;
        _aiService = aiService;
        _contentRepo = contentRepo;
    }

    public async Task<DTOSearchRequest> GetResults(string query)
    {
        _logger.LogDebug("Getting results for {query}...", query);
        var searchRequest = new DTOSearchRequest(query);
        searchRequest.QueryVector = await VectorizeQuery(query);
        _logger.LogDebug("Getting proximal embedding for {query}...", searchRequest.SearchQuery);
        searchRequest.ProximalEmbeddings = await GetProximalEmbeddings(searchRequest);
        return searchRequest;
    }

    private async Task<Vector> VectorizeQuery(string query)
    {
        _logger.LogDebug("Vectorizing query: {query}...", query);
        return await _aiService.GetSearchVectorAsync(query);
    }

    private async Task<List<TextEmbedding>> GetProximalEmbeddings(DTOSearchRequest searchRequest, int topK = 25)
    {
        _logger.LogDebug("Finding {topK} most similar embeddings...", topK);

        var results = await _contentRepo.GetSimilarEmbeddingsAsync(searchRequest.QueryVector,
            topK);
        var resultsList = results.ToList();
        _logger.LogDebug("Found {topK} most similar embeddings.", resultsList.Count);
        return results.ToList();
    }
}