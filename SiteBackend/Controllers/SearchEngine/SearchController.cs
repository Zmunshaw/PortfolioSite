using Microsoft.AspNetCore.Mvc;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Services.Controllers;

namespace SiteBackend.Controllers.SearchEngine;

[ApiController]
[Route("search")]
public class SearchController(ISearchService searchService, ILogger<SearchController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string q,
        [FromQuery] int limit = 25,
        [FromQuery] string? site = null)
    {
        var thing = await searchService.GetResults(q);
        List<SearchResult> results = new();
        // Simulate creating a list of results up to maxResults
        for (var i = 0; i < thing.ProximalEmbeddings.Count; i++)
        {
            results.Add(new SearchResult
            {
                Id = 1.ToString(),
                Title = $"Result {i} for '{q}'" + (site != null ? $" on {site}" : ""),
                Url = $"http://example.com/{i}",
                Snippet = $"{thing.ProximalEmbeddings[i].RawText}"
            });
        }

        logger.LogInformation($"New search query for '{q}', maxResults: {limit}, site: {site}");

        return Ok(new { results });
    }
}