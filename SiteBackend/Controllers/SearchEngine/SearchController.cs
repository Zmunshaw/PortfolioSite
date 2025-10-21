using Microsoft.AspNetCore.Mvc;
using Pgvector;
using SiteBackend.Middleware.AIClient;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Services;

namespace SiteBackend.Controllers;
[ApiController]
[Route("search")]
public class SearchController(IAIService aiService, ILogger<SearchController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string q,
        [FromQuery] int limit = 30,
        [FromQuery] string? site = null)
    {
        Vector embeddings = new(await aiService.GetEmbeddingAsync(q));
        // Simulate creating a list of results up to maxResults
        var results = Enumerable.Range(1, limit).Select(i => new SearchResult
        {
            Id = i.ToString(),
            Title = $"Result {i} for '{q}'" + (site != null ? $" on {site}" : ""),
            Url = $"http://example.com/{i}",
            Snippet = $"Snippet {i}"
        }).ToList();

        logger.LogInformation($"New search query for '{q}', maxResults: {limit}, site: {site}");

        return Ok(new { results });
    }
}

