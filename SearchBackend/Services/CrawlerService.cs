using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SearchBackend.Repositories.SearchEngine.Interfaces;
using SiteBackend.Database;
using SiteBackend.DTO;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;

namespace SiteBackend.Services;

public class CrawlerService : ICrawlerService
{
    private readonly string _crawlerUrl = Environment.GetEnvironmentVariable("CRAWLER_URL")
                                          ?? "http://crawler-dev:9900";

    private readonly ILogger<CrawlerService> _logger;
    private readonly IPageRepo _pageRepo;
    private readonly IWebsiteRepo _websiteRepo;
    private readonly HttpClient _httpClient;
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;

    public CrawlerService(
        ILogger<CrawlerService> logger,
        IPageRepo pageRepo,
        IWebsiteRepo websiteRepo,
        HttpClient httpClient,
        IDbContextFactory<SearchEngineCtx> ctxFactory)
    {
        _logger = logger;
        _pageRepo = pageRepo;
        _websiteRepo = websiteRepo;
        _httpClient = httpClient;
        _ctxFactory = ctxFactory;
    }

    public async Task UpdateCrawlerDataAsync(DTOCrawlerData dtoPage)
    {
        _logger.LogDebug("Updating page");
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<DTOCrawlRequest>> GetEmptyPagesAsync(int amountToGet = 100)
    {
        // TODO: Add more robust logic for determining valid crawl candidates.
        var validPages = _pageRepo.GetPagesAsync(page => page.LastCrawlAttempt == null, amountToGet)
            .Result.ToList();

        foreach (var page in validPages)
            page.LastCrawlAttempt = DateTime.UtcNow;

        await _pageRepo.BatchUpdatePageAsync(validPages);

        var dtoPages = validPages.Select(pg => new DTOCrawlRequest(pg)).ToList();
        return dtoPages;
    }

    public async Task BatchUpdateCrawlerDataAsync(IEnumerable<DTOCrawlerData> pages)
    {
        var dtoCrawlerPages = pages as DTOCrawlerData[] ?? pages.ToArray();
        var pageList = dtoCrawlerPages.Select(pg => pg.ConvertToPage()).ToList();
        await _pageRepo.BatchUpdatePageAsync(pageList);
    }

    public async Task<DTOSitemapData> DiscoverSitemapAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering sitemap for {BaseUrl}", baseUrl);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_crawlerUrl}/map")
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(new { url = baseUrl }),
                    Encoding.UTF8,
                    "application/json")
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sitemap discovery returned {StatusCode} for {BaseUrl}", response.StatusCode, baseUrl);
                throw new HttpRequestException($"Sitemap discovery failed with status {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var sitemapData = JsonConvert.DeserializeObject<DTOSitemapData>(content);

            if (sitemapData == null)
            {
                _logger.LogWarning("Failed to parse sitemap data for {BaseUrl}", baseUrl);
                throw new InvalidOperationException("Failed to parse sitemap data");
            }

            _logger.LogInformation("Discovered sitemap with {UrlCount} URLs and {SitemapCount} nested sitemaps",
                sitemapData.UrlSet.Count, sitemapData.SitemapIndex.Count);

            return sitemapData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering sitemap for {BaseUrl}", baseUrl);
            throw;
        }
    }

    public async Task<DTOSitemapData> DiscoverAndScrapeSitemapAsync(
        string baseUrl,
        int batchSize = 50,
        int maxConcurrent = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering and scraping sitemap for {BaseUrl}", baseUrl);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_crawlerUrl}/scrape-sitemap")
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        url = baseUrl,
                        batchSize,
                        maxConcurrent
                    }),
                    Encoding.UTF8,
                    "application/json")
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sitemap scraping returned {StatusCode} for {BaseUrl}", response.StatusCode, baseUrl);
                throw new HttpRequestException($"Sitemap scraping failed with status {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

            if (result == null)
            {
                _logger.LogWarning("Failed to parse sitemap scrape result for {BaseUrl}", baseUrl);
                throw new InvalidOperationException("Failed to parse sitemap scrape result");
            }

            var totalUrls = result.ContainsKey("totalUrls") ? result["totalUrls"] : 0;
            var scrapedUrls = result.ContainsKey("scrapedUrls") ? result["scrapedUrls"] : 0;

            _logger.LogInformation("Scraped sitemap: {TotalUrls} total URLs, {ScrapedUrls} successfully scraped",
                totalUrls, scrapedUrls);

            // For now, just return a minimal DTOSitemapData
            // The scrape-sitemap endpoint returns scraped content, not sitemap structure
            // We'll need to call /map separately to get the sitemap structure
            return await DiscoverSitemapAsync(baseUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering and scraping sitemap for {BaseUrl}", baseUrl);
            throw;
        }
    }

    public async Task SaveSitemapAsync(int websiteId, DTOSitemapData sitemapData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving sitemap for website {WebsiteId}", websiteId);

        try
        {
            var website = await _websiteRepo.GetByIdAsync(websiteId, cancellationToken);
            if (website == null)
            {
                _logger.LogWarning("Website {WebsiteId} not found", websiteId);
                throw new InvalidOperationException($"Website {websiteId} not found");
            }

            // Convert DTO to entity
            var sitemap = ConvertDTOToSitemap(sitemapData, website);

            // Save using repository
            await _websiteRepo.AddSitemapAsync(sitemap, cancellationToken);

            _logger.LogInformation("Saved sitemap {SitemapId} with {UrlCount} URLs for website {WebsiteId}",
                sitemap.SitemapID, sitemap.UrlSet.Count, websiteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving sitemap for website {WebsiteId}", websiteId);
            throw;
        }
    }

    public async Task CreatePagesFromSitemapAsync(int sitemapId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating pages from sitemap {SitemapId}", sitemapId);

        try
        {
            await using var ctx = await _ctxFactory.CreateDbContextAsync(cancellationToken);

            var sitemap = await ctx.Sitemaps
                .Include(s => s.UrlSet)
                .Include(s => s.Website)
                .FirstOrDefaultAsync(s => s.SitemapID == sitemapId, cancellationToken);

            if (sitemap == null)
            {
                _logger.LogWarning("Sitemap {SitemapId} not found", sitemapId);
                throw new InvalidOperationException($"Sitemap {sitemapId} not found");
            }

            var pagesToCreate = new List<Page>();

            foreach (var url in sitemap.UrlSet)
            {
                // Create a new page for each URL
                var page = new Page
                {
                    WebsiteID = sitemap.WebsiteID,
                    Website = sitemap.Website,
                    Url = url,
                    Content = new Content
                    {
                        Title = "",
                        Text = ""
                    }
                };

                url.Page = page;
                pagesToCreate.Add(page);
            }

            await ctx.Pages.AddRangeAsync(pagesToCreate, cancellationToken);
            await ctx.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created {PageCount} pages from sitemap {SitemapId}",
                pagesToCreate.Count, sitemapId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pages from sitemap {SitemapId}", sitemapId);
            throw;
        }
    }

    private Sitemap ConvertDTOToSitemap(DTOSitemapData dto, Website website)
    {
        var sitemap = new Sitemap
        {
            WebsiteID = website.WebsiteID,
            Website = website,
            Location = dto.Location,
            LastModified = dto.LastModified,
            IsMapped = dto.IsMapped
        };

        // Convert URLs
        foreach (var urlDto in dto.UrlSet)
        {
            var url = new Url
            {
                Location = urlDto.Location,
                LastModified = urlDto.LastModified,
                ChangeFrequency = ParseChangeFrequency(urlDto.ChangeFrequency),
                Priority = urlDto.Priority,
                Sitemap = sitemap
            };

            // Convert media entries
            foreach (var mediaDto in urlDto.Media)
            {
                MediaEntry mediaEntry = mediaDto.Type.ToLower() switch
                {
                    "image" => new ImageEntry
                    {
                        Location = mediaDto.Location,
                        Type = MediaType.Image,
                        Url = url
                    },
                    "video" => new VideoEntry
                    {
                        Location = mediaDto.Location,
                        Type = MediaType.Video,
                        ThumbnailLocation = mediaDto.ThumbnailLocation,
                        Title = mediaDto.Title,
                        Description = mediaDto.Description,
                        ContentLocation = mediaDto.ContentLocation,
                        PlayerLocation = mediaDto.PlayerLocation,
                        Duration = ParseDuration(mediaDto.Duration),
                        Rating = mediaDto.Rating ?? 2.5f,
                        ViewCount = mediaDto.ViewCount,
                        PublicationDate = mediaDto.PublicationDate,
                        Restrictions = mediaDto.Restrictions,
                        Platform = mediaDto.Platform,
                        RequiresSubscription = mediaDto.RequiresSubscription,
                        Tag = mediaDto.Tag,
                        Url = url
                    },
                    "news" => new NewsEntry
                    {
                        Location = mediaDto.Location,
                        Type = MediaType.News,
                        Publication = mediaDto.Publication,
                        PublicationDate = mediaDto.PublicationDate,
                        Language = mediaDto.Language,
                        Title = mediaDto.Title,
                        Url = url
                    },
                    _ => throw new InvalidOperationException($"Unknown media type: {mediaDto.Type}")
                };

                url.Media.Add(mediaEntry);
            }

            sitemap.UrlSet.Add(url);
        }

        // Convert nested sitemaps (recursive)
        foreach (var nestedDto in dto.SitemapIndex)
        {
            var nestedSitemap = ConvertDTOToSitemap(nestedDto, website);
            nestedSitemap.ParentSitemap = sitemap;
            nestedSitemap.ParentSitemapId = sitemap.SitemapID;
            sitemap.SitemapIndex.Add(nestedSitemap);
        }

        return sitemap;
    }

    private ChangeFrequency? ParseChangeFrequency(string? freqString)
    {
        if (string.IsNullOrWhiteSpace(freqString))
            return null;

        return freqString.ToLower() switch
        {
            "always" => ChangeFrequency.Always,
            "hourly" => ChangeFrequency.Hourly,
            "daily" => ChangeFrequency.Daily,
            "weekly" => ChangeFrequency.Weekly,
            "monthly" => ChangeFrequency.Monthly,
            "yearly" => ChangeFrequency.Yearly,
            _ => ChangeFrequency.Unknown
        };
    }

    private TimeSpan? ParseDuration(string? durationString)
    {
        if (string.IsNullOrWhiteSpace(durationString))
            return null;

        // Try to parse as seconds
        if (int.TryParse(durationString, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        // Try to parse as TimeSpan
        if (TimeSpan.TryParse(durationString, out var duration))
        {
            return duration;
        }

        return null;
    }
}