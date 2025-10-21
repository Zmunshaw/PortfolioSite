using SiteBackend.Repositories.SearchEngine;

namespace SiteBackend.Singletons;

public class CrawlerManager : BackgroundService
{
    public static CrawlerManager Instance { get; }
    
    private readonly ILogger<CrawlerManager> _logger;
    private readonly IWebsiteRepo _websiteRepo;

    public CrawlerManager(ILogger<CrawlerManager> logger, IWebsiteRepo websiteRepo)
    {
        _logger = logger;
        _websiteRepo = websiteRepo;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Manager} running at: {time}", ToString(), DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Your long-running background task logic goes here
            _logger.LogInformation("Performing background task...");

            // Simulate work or a delay
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); 
        }

        _logger.LogInformation("{Manager} stopping at: {time}", ToString(), DateTimeOffset.Now);
    }
}