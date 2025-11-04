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

        foreach (var page in pages)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                await CrawlSingleWebpage(page, pageRepo, contentRepo, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to crawl website: {Url}", page.Url);
            }
        }

        _logger.LogInformation("Website crawl completed");
    }

    private async Task CrawlSingleWebpage(Page page, IPageRepo pageRepo, IContentRepo contentRepo,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Crawling page: {Url}", page.Url.Location);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_crawlerUrl}/scrape")
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(new[] { page.Url.Location }),
                    Encoding.UTF8,
                    "application/json")
            };

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.SendAsync(request, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crawling {Url}", page.Url.Location);
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Scraper returned {StatusCode} for {Url}", response.StatusCode, page.Url.Location);
                page.LastCrawled = DateTime.UtcNow;
                await pageRepo.UpdatePageAsync(page);
                return;
            }

            var content = await response.Content.ReadAsStringAsync(stoppingToken);
            var results = JsonConvert.DeserializeObject<List<DTOScraperResult>>(content);

            if (results == null || !results.Any())
            {
                _logger.LogWarning("No results from scraper for {Url}", page.Url.Location);
                page.LastCrawled = DateTime.UtcNow;
                await pageRepo.UpdatePageAsync(page);
                return;
            }

            var scraperResult = results.First();

            page.LastCrawlAttempt = DateTime.UtcNow;
            //page.Outlinks = scraperResult.Links.Select(lnk => new Url(lnk)).ToList();
            page.LastCrawled = DateTime.UtcNow;
            page.Content.Title = scraperResult.Title ?? "";
            page.Content.Description = scraperResult.Description ?? "";
            page.Content.Text = scraperResult.Content ?? "";

            page.LastCrawled = DateTime.UtcNow;
            await pageRepo.UpdatePageAsync(page);

            _logger.LogInformation("Stored content for {Url}", page.Url.Location);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error crawling {Url}", page.Url.Location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error crawling {Url}", page.Url.Location);
        }
    }
}