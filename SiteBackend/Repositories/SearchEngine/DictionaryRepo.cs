using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

public class DictionaryRepo : IDictionaryRepo
{
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    private readonly ILogger<DictionaryRepo> _logger;

    private readonly HashSet<string> _wordSet;
    private readonly SearchEngineCtx _ctx;

    public DictionaryRepo(ILogger<DictionaryRepo> logger, IDbContextFactory<SearchEngineCtx> ctxFactory)
    {
        _logger = logger;
        _ctxFactory = ctxFactory;

        _ctx = _ctxFactory.CreateDbContext();
        _wordSet = _ctx.Words.Select(wrd => wrd.Text).Distinct().ToHashSet();
    }

    public Task<IEnumerable<Word>> GetWords(Expression<Func<Word, bool>> predicate, int take, int skip = 0)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Content>> GetContents(Expression<Func<Word, bool>> predicate, int take, int skip = 0)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Page>> GetPages(Expression<Func<Word, bool>> predicate, int take, int skip = 0)
    {
        throw new NotImplementedException();
    }

    public bool RepoContains(string key)
    {
        return _wordSet.Contains(key);
    }

    public List<string> GetSimilarWords(string key)
    {
        throw new NotImplementedException();
    }

    public async Task AddWords(string[] words)
    {
    }
}