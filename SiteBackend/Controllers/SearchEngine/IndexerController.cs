using Microsoft.AspNetCore.Mvc;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;
using SiteBackend.Services;

namespace SiteBackend.Controllers.SearchEngine;

[ApiController]
[Route("indexer")]
public class IndexController : ControllerBase
{
    private readonly ILogger<IndexController> _logger;
    private readonly ISitemapService _sitemapService;
    private readonly IPageRepo _pageRepo;
    
    public IndexController(ILogger<IndexController> logger,  ISitemapService sitemapService, IPageRepo pageRepo)
    {
        _logger = logger;
        _sitemapService = sitemapService;
        _pageRepo = pageRepo;
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
    public async Task<ActionResult<Sitemap>> SubmitSitemap([FromBody] Sitemap newSitemap)
    {
        _logger.LogInformation("Received new sitemap");
        _logger.LogInformation(newSitemap.Location);

        await _sitemapService.AddSitemap(newSitemap);
        // Return a 201 Created status code with the location of the newly created resource
        return Created("Get fukt", newSitemap);
    }

    [RequestSizeLimit(5 * (100 * 1024 * 1024))]
    [HttpPost("submit-page")]
    public async Task<ActionResult<Page>> Page([FromBody] Page page)
    {
        // TODO Fixit: To fit the pattern this should page url to website sitemap
        _logger.LogDebug($"Recieved new page: {page.Content.Title}, ID: {page.PageID}");
        await _pageRepo.AddPageAsync(page);
        return Created("Get fukt", page);
    }
}