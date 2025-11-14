using System.Linq.Expressions;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public interface IDictionaryRepo
{
    Task<IEnumerable<Word>> GetWords(Expression<Func<Word, bool>> predicate, int take, int skip = 0, 
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Content>> GetContents(Expression<Func<Word, bool>> predicate, int take, int skip = 0,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Page>> GetPages(Expression<Func<Word, bool>> predicate, int take, int skip = 0,
        CancellationToken cancellationToken = default);

    bool RepoContains(string key);
}