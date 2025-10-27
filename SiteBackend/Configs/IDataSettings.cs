namespace SiteBackend.Configs;

public interface IDataSettings
{
    string SiteDBConn { get; }
    string SEDBConn { get; }
    string CacheConn { get; }
}