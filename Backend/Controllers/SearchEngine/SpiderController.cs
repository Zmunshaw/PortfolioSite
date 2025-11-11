using Microsoft.AspNetCore.Mvc;
using SiteBackend.DTO;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Services;

namespace SiteBackend.Controllers.SearchEngine;

[ApiController]
[Route("spider")]
public class SpiderController : ControllerBase
{
    private readonly ICrawlerService _crawlerService;
    private readonly ILogger<SpiderController> _logger;

    public SpiderController(ILogger<SpiderController> logger, ICrawlerService crawlerService)
    {
        _logger = logger;
        _crawlerService = crawlerService;
    }

    #region Scraping

    [RequestSizeLimit(20 * 100 * 1024 * 1024)]
    [HttpPost("scrape")]
    public async Task<IActionResult> SubmitPages([FromBody] List<DTOCrawlerData> pages)
    {
        _logger.LogInformation("Recieved page content submissions.....");

        await _crawlerService.BatchUpdateCrawlerDataAsync(pages);

        return Ok();
    }

    [HttpGet("scrape")]
    public async Task<IActionResult> Scrape()
    {
        _logger.LogInformation("Received scrape request.....");
        var pages = await _crawlerService.GetEmptyPagesAsync();
        pages = pages.ToList();
        _logger.LogDebug($"Got {pages.Count()} pages for scraping.");
        // TODO: Should return some result like a count of unique urls or smthn
        return Ok();
    }

    #endregion

    #region Sitemapping

    [RequestSizeLimit(10 * 100 * 1024 * 1024)]
    [HttpPost("map")]
    public async Task<IActionResult> SubmitSitemap([FromBody] Sitemap newSitemap)
    {
        _logger.LogInformation("Received new sitemap");
        _logger.LogInformation(newSitemap.Location);
        
        return Ok();
    }

    [HttpGet("map")]
    public async Task<IActionResult> MapRequest()
    {
        var sitemapTargets = _crawlerService.GetEmptyPagesAsync().Result.ToList();
        _logger.LogDebug($"Got {sitemapTargets.Count} Sitemap Targets for mapping.");
        return Ok(sitemapTargets);
    }

    #endregion

    #region Crawling

    [RequestSizeLimit(10 * 100 * 1024 * 1024)]
    [HttpPost("crawl")]
    public async Task<IActionResult> SubmitCrawl([FromBody] Sitemap newSitemap)
    {
        _logger.LogInformation("Received new sitemap");
        _logger.LogInformation(newSitemap.Location);
        
        return Ok();
    }

    [HttpGet("crawl")]
    public async Task<IActionResult> CrawlRequest()
    {
        var sitemapTargets = _crawlerService.GetEmptyPagesAsync().Result.ToList();
        _logger.LogDebug($"Got {sitemapTargets.Count} Sitemap Targets for mapping.");
        return Ok(sitemapTargets);
    }

    #endregion
}