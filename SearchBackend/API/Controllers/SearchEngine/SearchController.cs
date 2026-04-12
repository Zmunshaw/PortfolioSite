using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SiteBackend.DTO.Website;
using SiteBackend.Services.Controllers;

namespace SiteBackend.Controllers.SearchEngine;

[ApiController]
[Route("search")]
public class SearchController(ISearchService searchService, ILogger<SearchController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string q,
        [FromQuery] int pgsz = 25,
        [FromQuery] int crpg = 1,
        [FromQuery] string? site = null)
    {
        var request = new DTOSearchRequest(q, crpg, pgsz);

        var foundResults = await searchService.GetResults(request);

        logger.LogInformation($"New search query for '{q}', {crpg} Page Size: {pgsz}, site: {site}");

        var resp = Ok(new { foundResults });
        Console.WriteLine($"Resp: {JsonSerializer.Serialize(resp.Value)}");
        return resp;
    }
}