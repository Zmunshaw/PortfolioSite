using System.Data.Entity.Infrastructure;
using SiteBackend.Database;
using SiteBackend.Repositories.SearchEngine;

namespace SiteBackend.Singletons;

public class CrawlManager : BackgroundService
{
    private readonly string CRAWLER_URL = Environment.GetEnvironmentVariable("CRAWLER_URL");
    private readonly string CRAWLER_API_KEY = Environment.GetEnvironmentVariable("CRAWLER_API_KEY");
    
    private readonly ILogger<CrawlManager> _logger;
    private readonly IWebsiteRepo _websiteRepo;
    
    public CrawlManager(IWebsiteRepo siteRepo, ILogger<CrawlManager> logger)
    {
            
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Manager} running at: {time}", ToString(), DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            
        }
    }
}