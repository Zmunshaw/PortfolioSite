using Microsoft.EntityFrameworkCore;
using SiteBackend.Database;

namespace SiteBackend.Repositories.SearchEngine;

public class DictionaryRepo : IDictionaryRepo
{
    private readonly ILogger<DictionaryRepo> _logger;
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    private SearchEngineCtx _ctx;
    
    private readonly HashSet<string> _wordSet;

    public DictionaryRepo(ILogger<DictionaryRepo> logger, IDbContextFactory<SearchEngineCtx> ctxFactory)
    {
        _logger = logger;
        _ctxFactory = ctxFactory;
        
        _ctx = _ctxFactory.CreateDbContext();
        _wordSet = _ctx.Words.Select(wrd => wrd.Text).Distinct().ToHashSet();
    }

    public async Task AddWords(string[] words)
    {
        
    }
    
    public bool RepoContains(string key)
    {
        return _wordSet.Contains(key);
    }
}