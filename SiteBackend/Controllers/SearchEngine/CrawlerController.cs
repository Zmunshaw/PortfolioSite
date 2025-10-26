using Microsoft.AspNetCore.Mvc;
using SiteBackend.DTO;
using SiteBackend.Services;

namespace SiteBackend.Controllers.SearchEngine;

[ApiController]
[Route("crawler")]
public class CrawlerController : ControllerBase
{
    private readonly ICrawlerService _crawlerService;
    private readonly ILogger<CrawlerController> _logger;

    public CrawlerController(ILogger<CrawlerController> logger, ICrawlerService crawlerService)
    {
        _logger = logger;
        _crawlerService = crawlerService;
    }

    [RequestSizeLimit(20 * (100 * 1024 * 1024))]
    [HttpPost("scrape")]
    public async Task<IActionResult> SubmitPages([FromBody] List<DTOCrawlerData> pages)
    {
        _logger.LogInformation("Recieved page content submissions.....");

        await _crawlerService.BatchUpdateCrawlerDataAsync(pages);

        return Created("crawler", null);
    }

    [HttpGet("scrape")]
    public async Task<IActionResult> Scrape()
    {
        var pages = _crawlerService.GetEmptyPagesAsync().Result.ToList();
        _logger.LogDebug($"Got {pages.Count} pages for scraping.");
        return Ok(pages);
    }
}