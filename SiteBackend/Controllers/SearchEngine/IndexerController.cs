using Microsoft.AspNetCore.Mvc;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Services;

namespace SiteBackend.Controllers.SearchEngine;

[ApiController]
[Route("indexer")]
public class IndexController : ControllerBase
{
    private readonly ILogger<IndexController> _logger;
    private readonly ISitemapService _sitemapService;
    
    public IndexController(ILogger<IndexController> logger,  ISitemapService sitemapService)
    {
        _logger = logger;
        _sitemapService = sitemapService;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string q)
    {
        // Simulated results
        int indexResult = 12;
        Console.WriteLine($"New indexer query for '{q}'");
        return Ok(new { indexResult });
    }
    
    [RequestSizeLimit(10 * (100 * 1024 * 1024))]
    [HttpPost("submit-sitemap")]
    public ActionResult<Sitemap> SubmitSitemap([FromBody] Sitemap newSitemap)
    {
        _logger.LogInformation("Received new sitemap");
        _logger.LogInformation(newSitemap.Location);

        _sitemapService.AddSitemap(newSitemap);
        // Return a 201 Created status code with the location of the newly created resource
        return Created("Get fukt", newSitemap);
    }

    public ActionResult<Page> Page([FromQuery] Page page)
    {
        _logger.LogDebug($"Recieved new page: {page.Content.Title}, ID: {page.PageID}");
        
        return Created("Get fukt", page);
    }
}