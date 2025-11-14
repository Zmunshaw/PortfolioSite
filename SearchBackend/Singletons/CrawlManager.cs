using System.Text;
using Newtonsoft.Json;
using SiteBackend.DTO;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;

namespace SiteBackend.Singletons;

public class CrawlManager : BackgroundService
{
    private readonly string _crawlerUrl = Environment.GetEnvironmentVariable("CRAWLER_URL")
                                          ?? "http://crawler-dev:9900";

    private readonly HttpClient _httpClient;
    private readonly ILogger<CrawlManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CrawlManager(IServiceScopeFactory scopeFactory, ILogger<CrawlManager> logger, HttpClient httpClient)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Manager} running at: {time}", GetType().Name, DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CrawlWebPages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CrawlManager");
            }
        }
    }

    private async Task CrawlWebPages(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var pageRepo = scope.ServiceProvider.GetRequiredService<IPageRepo>();
        var contentRepo = scope.ServiceProvider.GetRequiredService<IContentRepo>();

        var pages = await pageRepo.GetPagesToCrawlAsync();

        if (pages == null || !pages.Any())
        {
            _logger.LogInformation("No pages to crawl");
            return;
        }

        _logger.LogInformation("Crawling {Count} pages", pages.Count());

        // Process pages in batches for better performance
        var batchSize = int.Parse(Environment.GetEnvironmentVariable("CRAWL_BATCH_SIZE") ?? "10");
        var pageList = pages.ToList();

        for (int i = 0; i < pageList.Count; i += batchSize)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            var batch = pageList.Skip(i).Take(batchSize).ToList();
            _logger.LogInformation("Processing batch {BatchNum}/{TotalBatches} ({BatchSize} pages)",
                (i / batchSize) + 1, (pageList.Count + batchSize - 1) / batchSize, batch.Count);

            try
            {
                await CrawlBatchOfPages(batch, pageRepo, contentRepo, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to crawl batch starting at index {Index}", i);
            }
        }

        _logger.LogInformation("Website crawl completed");
    }

    private async Task CrawlBatchOfPages(
        List<Page> pages,
        IPageRepo pageRepo,
        IContentRepo contentRepo,
        CancellationToken stoppingToken)
    {
        if (!pages.Any())
            return;

        var urls = pages.Select(p => p.Url.Location).ToList();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_crawlerUrl}/scrape")
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(urls),
                    Encoding.UTF8,
                    "application/json")
            };

            var response = await _httpClient.SendAsync(request, stoppingToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Scraper returned {StatusCode} for batch", response.StatusCode);
                // Mark all pages as attempted
                foreach (var page in pages)
                {
                    page.LastCrawlAttempt = DateTime.UtcNow;
                    page.CrawlAttempts++;
                }
                await pageRepo.BatchUpdatePageAsync(pages);
                return;
            }

            var content = await response.Content.ReadAsStringAsync(stoppingToken);
            var results = JsonConvert.DeserializeObject<List<DTOScraperResult>>(content);

            if (results == null || !results.Any())
            {
                _logger.LogWarning("No results from scraper for batch");
                foreach (var page in pages)
                {
                    page.LastCrawlAttempt = DateTime.UtcNow;
                    page.CrawlAttempts++;
                }
                await pageRepo.BatchUpdatePageAsync(pages);
                return;
            }

            // Match results to pages by URL
            var resultsByUrl = results.ToDictionary(r => r.Url, r => r);
            var updatedPages = new List<Page>();

            foreach (var page in pages)
            {
                if (resultsByUrl.TryGetValue(page.Url.Location, out var scraperResult))
                {
                    if (string.IsNullOrEmpty(scraperResult.Error))
                    {
                        // Core fields
                        page.Content.Title = scraperResult.Title ?? "";
                        page.Content.Description = scraperResult.Description ?? "";
                        page.Content.Text = scraperResult.Content ?? "";

                        // Enhanced metadata
                        page.Content.Author = scraperResult.Author;
                        page.Content.Language = scraperResult.Language;
                        page.Content.WordCount = scraperResult.WordCount;
                        page.Content.CanonicalUrl = scraperResult.Canonical;

                        // Parse dates
                        if (DateTime.TryParse(scraperResult.Published, out var publishedDate))
                        {
                            page.Content.PublishedDate = publishedDate;
                        }
                        if (DateTime.TryParse(scraperResult.Modified, out var modifiedDate))
                        {
                            page.Content.ModifiedDate = modifiedDate;
                        }

                        // Structured data as JSON
                        if (scraperResult.Headers != null)
                        {
                            page.Content.HeadersJson = JsonConvert.SerializeObject(scraperResult.Headers);
                        }
                        if (scraperResult.OpenGraph != null)
                        {
                            page.Content.OpenGraphJson = JsonConvert.SerializeObject(scraperResult.OpenGraph);
                        }
                        if (scraperResult.TwitterCard != null)
                        {
                            page.Content.TwitterCardJson = JsonConvert.SerializeObject(scraperResult.TwitterCard);
                        }

                        // Link analysis
                        page.Content.InternalLinkCount = scraperResult.InternalLinkCount;
                        page.Content.ExternalLinkCount = scraperResult.ExternalLinkCount;

                        page.LastCrawled = DateTime.UtcNow;
                        page.Content.LastScraped = DateTime.UtcNow;
                        _logger.LogDebug("Stored enhanced content for {Url} (Words: {WordCount}, Internal Links: {InternalLinks})",
                            page.Url.Location, page.Content.WordCount, page.Content.InternalLinkCount);
                    }
                    else
                    {
                        _logger.LogWarning("Error scraping {Url}: {Error}", page.Url.Location, scraperResult.Error);
                    }
                }

                page.LastCrawlAttempt = DateTime.UtcNow;
                page.CrawlAttempts++;
                updatedPages.Add(page);
            }

            // Batch update all pages at once
            if (updatedPages.Any())
            {
                await pageRepo.BatchUpdatePageAsync(updatedPages);
                _logger.LogInformation("Updated {Count} pages from batch", updatedPages.Count);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error crawling batch");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error crawling batch");
        }
    }

}