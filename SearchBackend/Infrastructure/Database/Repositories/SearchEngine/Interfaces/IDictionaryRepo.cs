using System.Linq.Expressions;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public interface IDictionaryRepo
{
    Task<IEnumerable<Word>> GetWords(Expression<Func<Word, bool>> predicate, int take, int skip = 0);
    Task<IEnumerable<Content>> GetContents(Expression<Func<Word, bool>> predicate, int take, int skip = 0);
    Task<IEnumerable<Page>> GetPages(Expression<Func<Word, bool>> predicate, int take, int skip = 0);

    bool RepoContains(string key);
    List<string> GetSimilarWords(string key);
}