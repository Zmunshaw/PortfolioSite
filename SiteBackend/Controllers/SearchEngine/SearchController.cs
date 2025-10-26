using System.Text.Json;
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
        var foundResults = await searchService.GetResults(q);
        List<SearchResult> results = new();
        // Simulate creating a list of results up to maxResults
        for (var i = 0; i < foundResults.ProximalEmbeddings.Count; i++)
        {
            var resTitle = foundResults.ProximalEmbeddings[i].Content?.Title;
            var resLink = foundResults.ProximalEmbeddings[i].Content?.Page.Url.Location;
            var resSnippet = string.Join(" ", foundResults.ProximalEmbeddings[i].Content?.Text.Split(' ').Take(30));
            results.Add(new SearchResult
            {
                Id = i.ToString(),
                Title = resTitle,
                Url = resLink,
                Snippet = $"{resSnippet}"
            });
        }

        logger.LogInformation($"New search query for '{q}', maxResults: {limit}, site: {site}");

        var resp = Ok(new { results });
        Console.WriteLine($"Resp: {JsonSerializer.Serialize(resp.Value)}");
        return resp;
    }
}