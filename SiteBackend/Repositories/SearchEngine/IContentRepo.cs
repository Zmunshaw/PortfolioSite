using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public interface IContentRepo
{
    Task AddContentAsync(Content Content);
    Task BatchAddContentAsync(IEnumerable<Content> Contents);
    
    Task<Content?> GetContentAsync(Func<Content, bool> predicate);
    Task<IEnumerable<Content>> GetContentsAsync(Func<Content, bool> predicate);
    Task<IEnumerable<Content>> GetContentsAsync(Func<Content, bool> predicate, int take, int skip = 0);
    
    Task UpdateContentAsync(Content content);
    Task BatchUpdateContentAsync(IEnumerable<Content> contents);
    
    Task DeleteContentAsync(Content content);
    
    Task SaveChangesAsync(bool clearCtxOnSave = true);
}