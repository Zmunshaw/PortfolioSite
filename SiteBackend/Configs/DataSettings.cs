namespace SiteBackend.Configs;

public class DataSettings : IDataSettings
{
    public DataSettings(ILogger<DataSettings> logger, IConfiguration config)
    {
        logger.LogInformation("Loading data settings");
        var siteDbConn = Environment.GetEnvironmentVariable("SITE_DB_CONN")?.Trim();
        var seDbConn = Environment.GetEnvironmentVariable("SE_DB_CONN")?.Trim();
        var cacheConn = Environment.GetEnvironmentVariable("CACHE_CONN")?.Trim();
        logger.LogInformation($"Site DB ConnStr {siteDbConn}\n SE DB ConnStr {seDbConn}\n Cache ConnStr {cacheConn}");
        SiteDBConn = siteDbConn ?? "";
        SEDBConn = seDbConn ?? "";
        CacheConn = cacheConn ?? "";
        logger.LogWarning("FUG!");
    }

    public string SiteDBConn { get; }

    public string SEDBConn { get; }

    public string CacheConn { get; }
}