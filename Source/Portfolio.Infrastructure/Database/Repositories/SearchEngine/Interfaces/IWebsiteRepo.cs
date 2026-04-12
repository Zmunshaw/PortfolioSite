namespace Portfolio.Infrastructure.Database.Repositories.SearchEngine.Interfaces;

public interface IWebsiteRepo
{
    Task<IEnumerable<Website>> GetAllAsync();
    Task<Website?> GetByIdAsync(int id);
    Task<Website?> GetByHostNameAsync(string hostName);
    Task AddWebsiteAsync(Website website);
    Task AddSitemapAsync(Sitemap sitemap);

    void UpdateWebsite(Website website);
    void UpdateSitemap(Sitemap sitemap);

    void DeleteWebsite(Website website);
    void DeleteSitemap(Sitemap sitemap);
    Task<bool> SaveChangesAsync();
}