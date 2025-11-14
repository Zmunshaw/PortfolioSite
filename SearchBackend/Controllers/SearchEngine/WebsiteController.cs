using Microsoft.AspNetCore.Mvc;
using SearchBackend.Repositories.SearchEngine.Interfaces;
using SiteBackend.DTO;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Services;

namespace SiteBackend.Controllers.SearchEngine;

[ApiController]
[Route("websites")]
public class WebsiteController : ControllerBase
{
    private readonly ILogger<WebsiteController> _logger;
    private readonly IWebsiteRepo _websiteRepo;
    private readonly ICrawlerService _crawlerService;

    public WebsiteController(
        ILogger<WebsiteController> logger,
        IWebsiteRepo websiteRepo,
        ICrawlerService crawlerService)
    {
        _logger = logger;
        _websiteRepo = websiteRepo;
        _crawlerService = crawlerService;
    }

    /// <summary>
    /// Get all tracked websites
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllWebsites(CancellationToken cancellationToken = default)
    {
        try
        {
            var websites = await _websiteRepo.GetAllAsync(cancellationToken);

            var result = websites.Select(w => new
            {
                w.WebsiteID,
                w.Host,
                w.SitemapID,
                SitemapMapped = w.Sitemap?.IsMapped ?? false,
                SitemapUrlCount = w.Sitemap?.UrlSet.Count ?? 0,
                PageCount = w.Pages.Count,
                SitemapLastModified = w.Sitemap?.LastModified
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all websites");
            return StatusCode(500, new { error = "Failed to retrieve websites" });
        }
    }

    /// <summary>
    /// Get a specific website by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWebsiteById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var website = await _websiteRepo.GetByIdAsync(id, cancellationToken);

            if (website == null)
            {
                return NotFound(new { error = $"Website {id} not found" });
            }

            var result = new
            {
                website.WebsiteID,
                website.Host,
                website.SitemapID,
                Sitemap = website.Sitemap != null ? new
                {
                    website.Sitemap.SitemapID,
                    website.Sitemap.Location,
                    website.Sitemap.LastModified,
                    website.Sitemap.IsMapped,
                    UrlCount = website.Sitemap.UrlSet.Count,
                    NestedSitemapCount = website.Sitemap.SitemapIndex.Count
                } : null,
                PageCount = website.Pages.Count,
                Pages = website.Pages.Select(p => new
                {
                    p.PageID,
                    Url = p.Url?.Location,
                    p.LastCrawled,
                    p.CrawlAttempts,
                    HasContent = p.Content?.Text?.Length > 0
                }).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting website {Id}", id);
            return StatusCode(500, new { error = "Failed to retrieve website" });
        }
    }

    /// <summary>
    /// Add a new website for tracking
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddWebsite([FromBody] DTOWebsiteRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request?.Host))
        {
            return BadRequest(new { error = "Host is required" });
        }

        try
        {
            // Check if website already exists
            var existing = await _websiteRepo.GetByHostNameAsync(request.Host, cancellationToken);
            if (existing != null)
            {
                return Conflict(new
                {
                    error = "Website already exists",
                    websiteId = existing.WebsiteID,
                    host = existing.Host
                });
            }

            // Create new website
            var website = new Website
            {
                Host = request.Host
            };

            await _websiteRepo.AddWebsiteAsync(website, cancellationToken);

            _logger.LogInformation("Added new website: {Host} with ID {WebsiteId}", website.Host, website.WebsiteID);

            return CreatedAtAction(
                nameof(GetWebsiteById),
                new { id = website.WebsiteID },
                new
                {
                    website.WebsiteID,
                    website.Host,
                    message = "Website added successfully. Sitemap will be discovered automatically."
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding website {Host}", request.Host);
            return StatusCode(500, new { error = "Failed to add website" });
        }
    }

    /// <summary>
    /// Manually trigger sitemap discovery for a website
    /// </summary>
    [HttpPost("{id}/discover-sitemap")]
    public async Task<IActionResult> DiscoverSitemap(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var website = await _websiteRepo.GetByIdAsync(id, cancellationToken);
            if (website == null)
            {
                return NotFound(new { error = $"Website {id} not found" });
            }

            _logger.LogInformation("Manually discovering sitemap for website {WebsiteId}: {Host}", id, website.Host);

            // Ensure the host has a protocol
            var baseUrl = website.Host.StartsWith("http") ? website.Host : $"https://{website.Host}";

            // Discover the sitemap
            var sitemapData = await _crawlerService.DiscoverSitemapAsync(baseUrl, cancellationToken);

            // Save the sitemap
            await _crawlerService.SaveSitemapAsync(id, sitemapData, cancellationToken);

            // Get the saved sitemap ID
            var updatedWebsite = await _websiteRepo.GetByIdAsync(id, cancellationToken);
            if (updatedWebsite?.Sitemap != null)
            {
                // Create pages from sitemap
                await _crawlerService.CreatePagesFromSitemapAsync(updatedWebsite.Sitemap.SitemapID, cancellationToken);
            }

            return Ok(new
            {
                message = "Sitemap discovered and saved successfully",
                urlCount = sitemapData.UrlSet.Count,
                nestedSitemapCount = sitemapData.SitemapIndex.Count,
                sitemapLocation = sitemapData.Location
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error discovering sitemap for website {Id}", id);
            return StatusCode(502, new { error = "Failed to fetch sitemap from website" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering sitemap for website {Id}", id);
            return StatusCode(500, new { error = "Failed to discover sitemap" });
        }
    }

    /// <summary>
    /// Delete a website and all associated data
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebsite(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var website = await _websiteRepo.GetByIdAsync(id, cancellationToken);
            if (website == null)
            {
                return NotFound(new { error = $"Website {id} not found" });
            }

            await _websiteRepo.DeleteWebsiteAsync(website, cancellationToken);

            _logger.LogInformation("Deleted website {WebsiteId}: {Host}", id, website.Host);

            return Ok(new { message = $"Website {website.Host} deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting website {Id}", id);
            return StatusCode(500, new { error = "Failed to delete website" });
        }
    }
}
