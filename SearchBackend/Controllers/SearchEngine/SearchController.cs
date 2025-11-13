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
        DTOSearchRequest request;
        IEnumerable<DTOSearchResult> searchResults;
        
        try
        {
            request = new DTOSearchRequest(q, crpg, pgsz, site);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning($"Caught Bad request on SearchController {ex.Message}");
            return BadRequest(ex.Message);
        }

        try
        {
            searchResults = await searchService.GetResults(request);
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Caught Failure to build results on SearchController {ex.Message}");
            return StatusCode(500, "Failed to retrieve search results");
        }

        var resp = Ok(searchResults);
        logger.LogDebug($"Resp: {JsonSerializer.Serialize(resp.Value)}");
        return resp;
    }
}