using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Registry;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Repositories.SearchEngine;

/// <summary>
/// Repository for word dictionary operations with.
/// </summary>
public class DictionaryRepo : IDictionaryRepo
{
    private readonly IDbContextFactory<SearchEngineCtx> _ctxFactory;
    private readonly ILogger<DictionaryRepo> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    
    private readonly HashSet<string> _wordSet;

    public DictionaryRepo(
        ILogger<DictionaryRepo> logger,
        IDbContextFactory<SearchEngineCtx> ctxFactory,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _logger = logger;
        _ctxFactory = ctxFactory;
        _resiliencePipeline = pipelineProvider.GetPipeline("db-backoff");

        using var ctx = _ctxFactory.CreateDbContext();
        _wordSet = ctx.Words.Select(wrd => wrd.Text).Distinct().ToHashSet();
        _logger.LogInformation("DictionaryRepo initialized with {WordCount} words", _wordSet.Count);
    }

    /// <summary>
    /// Gets words matching the provided predicate with pagination.
    /// Includes automatic retry logic via Polly for transient failures.
    /// </summary>
    public async Task<IEnumerable<Word>> GetWords(Expression<Func<Word, bool>> predicate, int take, int skip = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var words = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);

                return await ctx.Words
                    .AsNoTracking()
                    .Where(predicate)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(ct);
            }, cancellationToken);

            _logger.LogDebug("Retrieved {WordCount} words matching predicate", words.Count);
            return words;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get words matching predicate after retries");
            throw;
        }
    }

    /// <summary>
    /// Gets content items that contain words matching the provided predicate with pagination.
    /// Uses the many-to-many relationship between Word and Content.
    /// Includes automatic retry logic via Polly for transient failures.
    /// </summary>
    public async Task<IEnumerable<Content>> GetContents(Expression<Func<Word, bool>> predicate, int take, int skip = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contents = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);

                return await ctx.Words
                    .AsNoTracking()
                    .Where(predicate)
                    .SelectMany(w => w.Contents)
                    .Distinct()
                    .Include(c => c.Page)
                    .Include(c => c.Embeddings)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(ct);
            }, cancellationToken);

            _logger.LogDebug("Retrieved {ContentCount} content items", contents.Count);
            return contents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get contents for words matching predicate after retries");
            throw;
        }
    }

    /// <summary>
    /// Gets pages that contain words matching the provided predicate with pagination.
    /// Navigates from Word → Content → Page.
    /// Includes automatic retry logic via Polly for transient failures.
    /// </summary>
    public async Task<IEnumerable<Page>> GetPages(
        Expression<Func<Word, bool>> predicate,
        int take,
        int skip = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pages = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await _ctxFactory.CreateDbContextAsync(ct);

                return await ctx.Words
                    .AsNoTracking()
                    .Where(predicate)
                    .SelectMany(w => w.Contents)
                    .Select(c => c.Page)
                    .Distinct()
                    .Include(p => p.Content)
                    .Include(p => p.Url)
                    .Include(p => p.Website)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(ct);
            }, cancellationToken);

            _logger.LogDebug("Retrieved {PageCount} pages containing matching words", pages.Count);
            return pages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pages for words matching predicate after retries");
            throw;
        }
    }

    /// <summary>
    /// Fast in-memory check if a word exists in the dictionary.
    /// Uses the HashSet loaded at initialization for O(1) lookup.
    /// No database call needed, so no retry logic required.
    /// </summary>
    public bool RepoContains(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return _wordSet.Contains(key.ToLower());
    }
}