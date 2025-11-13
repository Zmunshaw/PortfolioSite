using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Registry;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;

namespace SearchBackend.Repositories.SearchEngine;

public class ContentRepo(ILogger<ContentRepo> logger, IDbContextFactory<SearchEngineCtx> ctxFactory, 
    ResiliencePipelineProvider<string> pipelineProvider) : IContentRepo
{
    private readonly ResiliencePipeline _resiliencePipeline = pipelineProvider.GetPipeline("db-backoff");

    public async Task AddContentAsync(Content content, CancellationToken cancellationToken = default)
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await ctxFactory.CreateDbContextAsync(ct);
                
                ctx.Contents.Add(content);
                await ctx.SaveChangesAsync(ct);
            }, cancellationToken);
            
            logger.LogInformation("Content added successfully with ID: {ContentId}", content.ContentID);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add content: {ContentTitle}", content.Title);
            throw;
        }
    }

    public async Task BatchAddContentAsync(IEnumerable<Content> contents, CancellationToken cancellationToken = default,
        BulkConfig? blkConfig = null)
    {
        var contentList = contents.ToList();
        
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await ctxFactory.CreateDbContextAsync(ct);
                await ctx.BulkInsertAsync(contentList, blkConfig, cancellationToken: ct);
            }, cancellationToken);
            
            logger.LogInformation("Batch insert completed. Added {ContentCount} items", contentList.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to batch insert {ContentCount} contents", contentList.Count);
            throw;
        }
    }

    public async Task<Content?> GetContentAsync(
        Expression<Func<Content, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await ctxFactory.CreateDbContextAsync(ct);

                return await ctx.Contents
                    .AsNoTracking()
                    .Include(c => c.Embeddings)
                    .FirstOrDefaultAsync(predicate, ct);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve single content");
            throw;
        }
    }

    public async Task<IEnumerable<Content>> GetContentsAsync(Expression<Func<Content, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await ctxFactory.CreateDbContextAsync(ct);

                return ctx.Contents
                    .AsNoTracking()
                    .Where(predicate)
                    .Include(c => c.Embeddings);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve contents");
            throw;
        }
    }

    public async Task<IEnumerable<Content>> GetContentsAsync(Expression<Func<Content, bool>> predicate, int take, 
        int skip = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await ctxFactory.CreateDbContextAsync(ct);

                return await ctx.Contents
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(predicate)
                    .Include(c => c.Embeddings)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(ct);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Failed to retrieve paginated contents with skip: {Skip}, take: {Take}", skip, take);
            throw;
        }
    }

    public async Task UpdateContentAsync(Content content, CancellationToken cancellationToken = default)
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await ctxFactory.CreateDbContextAsync(ct);

                ctx.Contents.Update(content);
                await ctx.SaveChangesAsync(ct);
            }, cancellationToken);
            
            logger.LogInformation("Content updated successfully with ID: {ContentId}", content.ContentID);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update content with ID: {ContentId}", content.ContentID);
            throw;
        }
    }

    public async Task BatchUpdateContentAsync(IEnumerable<Content> contents,
        CancellationToken cancellationToken = default, BulkConfig? blkConfig = null)
    {
        var contentList = contents.ToList();
        
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await ctxFactory.CreateDbContextAsync(ct);
                await ctx.BulkInsertOrUpdateAsync(contentList, blkConfig, cancellationToken: ct);
                ctx.BulkSaveChanges(blkConfig);
            }, cancellationToken);
            
            logger.LogInformation("Batch update completed. Updated {ContentCount} items", contentList.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to batch update {ContentCount} contents", contentList.Count);
            throw;
        }
    }

    public async Task DeleteContentAsync(Content content, CancellationToken cancellationToken = default)
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await using var ctx = await ctxFactory.CreateDbContextAsync(ct);
                ctx.Contents.Remove(content);
                await ctx.SaveChangesAsync(ct);
            }, cancellationToken);
            
            logger.LogInformation("Content deleted successfully with ID: {ContentId}", content.ContentID);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete content with ID: {ContentId}", content.ContentID);
            throw;
        }
    }
}