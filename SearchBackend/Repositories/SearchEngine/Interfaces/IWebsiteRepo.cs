using SiteBackend.Models.SearchEngine.Index;

namespace SearchBackend.Repositories.SearchEngine.Interfaces;

/// <summary>
/// Repository interface for website and sitemap operations.
/// Provides CRUD operations with resilience patterns and full async support.
/// </summary>
public interface IWebsiteRepo
{
    /// <summary>
    /// Gets all websites from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of all websites with their sitemaps and pages</returns>
    Task<IEnumerable<Website>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a website by ID.
    /// </summary>
    /// <param name="id">Website ID</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Website if found, null otherwise</returns>
    Task<Website?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a website by hostname (case-insensitive search).
    /// </summary>
    /// <param name="hostName">Hostname to search for</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Website if found, null otherwise</returns>
    Task<Website?> GetByHostNameAsync(
        string hostName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new website to the database.
    /// </summary>
    /// <param name="website">Website to add</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task AddWebsiteAsync(Website website, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a sitemap to the database and creates its associated website if needed.
    /// </summary>
    /// <param name="sitemap">Sitemap to add</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task AddSitemapAsync(Sitemap sitemap, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing website.
    /// </summary>
    /// <param name="website">Website with updated values</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task UpdateWebsiteAsync(Website website, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing sitemap.
    /// </summary>
    /// <param name="sitemap">Sitemap with updated values</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task UpdateSitemapAsync(Sitemap sitemap, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a website and all associated data (pages, sitemaps, URLs).
    /// </summary>
    /// <param name="website">Website to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task DeleteWebsiteAsync(Website website, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a sitemap and all associated URLs.
    /// </summary>
    /// <param name="sitemap">Sitemap to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task DeleteSitemapAsync(Sitemap sitemap, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves any pending changes to the database.
    /// Only needed for manual change tracking; most operations auto-save.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if changes were saved, false if nothing to save</returns>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}