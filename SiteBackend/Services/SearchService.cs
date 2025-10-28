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
        (searchRequest.DenseVector, searchRequest.SparseVector) = await VectorizeQuery(query);
        searchRequest.SearchResults = await GetRankedResults(searchRequest);
        return searchRequest;
    }

    private async Task<(Vector, SparseVector)> VectorizeQuery(string query)
    {
        _logger.LogDebug("Vectorizing query: {query}...", query);
        var denseTask = _aiService.GetDenseSearchVectorAsync(query);
        var sparseTask = _aiService.GetSparseSearchVectorAsync(query);

        await Task.WhenAll(denseTask, sparseTask);

        return (denseTask.Result, sparseTask.Result);
    }

    // TODO IMPROVE: finding common denominators probably wont result in much
    private async Task<List<DTOSearchResult>> GetRankedResults(DTOSearchRequest request)
    {
        var (sparseRes, denseRes) = await Task.WhenAll(
                GetProximalSparsePages(request.SparseVector),
                GetProximalDensePages(request.DenseVector)
            )
            .ContinueWith(res => (res.Result[0], res.Result[1]));

        _logger.LogDebug("Got {sparseRes} sparse results and {denseRes} dense results.",
            sparseRes.Count, denseRes.Count);

        var denseSet = denseRes.ToHashSet(); // Assuming Id uniqueness
        var results = new List<DTOSearchResult>();

        var reranked = sparseRes
            .Where(pg => denseSet.Contains(pg))
            .Select(pg => new DTOSearchResult(pg))
            .ToList();
        results.AddRange(reranked);

        results.AddRange(sparseRes
            .Where(pg => !denseSet.Contains(pg))
            .Select(pg => new DTOSearchResult(pg)));

        results.AddRange(denseRes
            .Where(pg => !sparseRes.Select(s => s).Contains(pg))
            .Select(pg => new DTOSearchResult(pg)));

        return results.Take(20).ToList();
    }

    private async Task<List<Page>> GetProximalSparsePages(SparseVector vector, int topK = 25)
    {
        _logger.LogDebug("Finding {topK} most similar embeddings...", topK);

        var results = await _contentRepo.GetSimilarSparseEmbeddingsAsync(vector, topK);
        var resultsList = results.ToList();
        _logger.LogDebug("Found {topK} most similar embeddings.", resultsList.Count);
        return results.ToList();
    }

    private async Task<List<Page>> GetProximalDensePages(Vector vector, int topK = 25)
    {
        _logger.LogDebug("Finding {topK} most similar embeddings...", topK);
        var results = await _contentRepo.GetSimilarDenseEmbeddingsAsync(vector, topK);
        var resultsList = results.ToList();
        _logger.LogDebug("Found {topK} most similar embeddings.", resultsList.Count);
        return results.ToList();
    }
}