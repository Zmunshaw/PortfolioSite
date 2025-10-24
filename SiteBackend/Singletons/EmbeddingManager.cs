using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.VisualBasic;
using SiteBackend.Middleware.AIClient;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;
using SiteBackend.Services;

namespace SiteBackend.Singletons;

public class EmbeddingManager : BackgroundService
{
    private readonly ILogger<EmbeddingManager> _logger;
    private readonly IAIService _aiService;
    private readonly IContentRepo _contentRepo;
    private readonly IDictionaryRepo _dictionaryRepo;
    SHA256 _hasher = SHA256.Create();

    private const int MinWordSize = 2;
    private const int ChunkSize = 500;
    public EmbeddingManager(IServiceScopeFactory scopeFactory)
    {
        var curScope = scopeFactory.CreateScope();
        
        _logger = curScope.ServiceProvider.GetRequiredService<ILogger<EmbeddingManager>>();
        _aiService = curScope.ServiceProvider.GetRequiredService<IAIService>();
        _contentRepo = curScope.ServiceProvider.GetRequiredService<IContentRepo>();
        _dictionaryRepo = curScope.ServiceProvider.GetRequiredService<IDictionaryRepo>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Manager} running at: {time}", ToString(), DateTimeOffset.Now);
        Task? updatePageEmbeddingTask = null;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            // Your long-running background task logic goes here
            _logger.LogInformation("Performing background task...");
            if (updatePageEmbeddingTask == null || updatePageEmbeddingTask.IsCompletedSuccessfully)
            {
                updatePageEmbeddingTask = UpdatePageEmbeddings();
            }
            else
            {
                _logger.LogWarning("Something went wrong while updating page embeddings.");
                if (updatePageEmbeddingTask.IsFaulted)
                    _logger.LogDebug(updatePageEmbeddingTask.Exception.ToString());
                
                updatePageEmbeddingTask = null;
            }
            // Simulate work or a delay
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); 
        }

        _logger.LogInformation("{Manager} stopping at: {time}", ToString(), DateTimeOffset.Now);
    }

    async Task UpdatePageEmbeddings()
    {
        IEnumerable<Content> updateList = await _contentRepo
            .GetContentsAsync(ct => ct.NeedsEmbedding && !string.IsNullOrEmpty(ct.Text), 100);
        var enumerable = updateList as Content[] ?? updateList.ToArray();
        
        _logger.LogDebug("Found {ContentCount} pages that need new embeddings", enumerable.Length);

        foreach (var content in enumerable)
        {
            _logger.LogDebug("Fixing Text...");
            var wordArray = StripInvalidWords(content.Text);
            var wordChunks = ChunkWords(wordArray);
            
            content.Embeddings = await _aiService.EmbedDocumentAsync(content.Title, wordChunks);
            content.Embeddings.Select(emb => emb.EmbeddingHash = ComputeContentHash(emb.RawText));
            
            _logger.LogDebug($"Hash generated: {content.ContentHash}");
            content.ContentHash = ComputeContentHash(content.Text);
        }
        
        await _contentRepo.BatchUpdateContentAsync(updateList);
    }

    private string[] StripInvalidWords(string text)
    {
        var words = text.Split([' '], StringSplitOptions.RemoveEmptyEntries);
    
        var validWords = words
            .Where(word => word.Length >= MinWordSize && _dictionaryRepo.RepoContains(word)).ToArray();

        return validWords;
    }

    private string[][] ChunkWords(string[] wordArray)
    {
        int chunkCount = (int)Math.Ceiling((double)wordArray.Length / ChunkSize);
        var chunks = new string[chunkCount][];
        
        for (int i = 0; i < chunkCount; i++)
        {
            chunks[i] = wordArray.Skip(i * ChunkSize).Take(ChunkSize).ToArray();
        }

        return chunks;
    }
    
    private string ComputeContentHash(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = _hasher.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}