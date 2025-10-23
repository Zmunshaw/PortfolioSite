using Microsoft.AspNetCore.Mvc;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Services;

namespace SiteBackend.Controllers.SearchEngine;

[ApiController]
[Route("crawler")]
public class CrawlerController : ControllerBase
{
    private readonly ILogger<CrawlerController> _logger;
    private readonly ICrawlerService  _crawlerService;

    public CrawlerController(ILogger<CrawlerController> logger, ICrawlerService crawlerService)
    {
        _logger = logger;
        _crawlerService = crawlerService;
    }

    [RequestSizeLimit(20 * (100 * 1024 * 1024))]
    [HttpPost("scrape")]
    public async Task<IActionResult> SubmitPages([FromBody] List<Page> pages)
    {
        await _crawlerService.BatchUpdatePagesAsync(pages);
        
        return Created("crawler", null);
    }

    [HttpGet("scrape")]
    public async Task<IActionResult> Scrape()
    {
        var pages = await _crawlerService.GetEmptyPagesAsync();
        return Ok(pages);
    }
}