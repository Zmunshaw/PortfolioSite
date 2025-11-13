using System.Linq.Expressions;
using EFCore.BulkExtensions;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public interface IContentRepo
{
    Task AddContentAsync(Content content,
        CancellationToken cancellationToken = default);
    Task BatchAddContentAsync(IEnumerable<Content> contents, 
        CancellationToken cancellationToken = default, BulkConfig? bulkConfig = null);

    Task<Content?> GetContentAsync(Expression<Func<Content, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Content>> GetContentsAsync(Expression<Func<Content, bool>> predicate, 
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Content>> GetContentsAsync(Expression<Func<Content, bool>> predicate, int take, int skip = 0,
        CancellationToken cancellationToken = default);

    Task UpdateContentAsync(Content content,
        CancellationToken cancellationToken = default);
    Task BatchUpdateContentAsync(IEnumerable<Content> contents,
        CancellationToken cancellationToken = default, BulkConfig? bulkConfig = null);

    Task DeleteContentAsync(Content content,
        CancellationToken cancellationToken = default);
}