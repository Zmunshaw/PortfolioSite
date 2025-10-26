using System.Security.Cryptography;
using System.Text;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;
using SiteBackend.Services;

namespace SiteBackend.Singletons;

public class EmbeddingManager : BackgroundService
{
    private const int MinWordSize = 2;
    private const int ChunkSize = 500;
    private readonly IAIService _aiService;
    private readonly IContentRepo _contentRepo;
    private readonly IDictionaryRepo _dictionaryRepo;
    private readonly ILogger<EmbeddingManager> _logger;
    SHA256 _hasher = SHA256.Create();
    private int currentPage;

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
            if (updatePageEmbeddingTask == null || updatePageEmbeddingTask.IsCompletedSuccessfully)
            {
                updatePageEmbeddingTask = UpdatePageEmbeddings();
            }
            else if (updatePageEmbeddingTask != null && updatePageEmbeddingTask.IsFaulted)
            {
                _logger.LogWarning("Updating page embedding task faulted: {error}", updatePageEmbeddingTask.Exception);
                updatePageEmbeddingTask = null;
            }
            else if (updatePageEmbeddingTask != null && updatePageEmbeddingTask.IsCanceled)
            {
                _logger.LogWarning("Updating page embedding task canceled: {error}", updatePageEmbeddingTask.Exception);
            }

            if (updatePageEmbeddingTask == null)
            {
                _logger.LogDebug("Updating page embedding task was null...");

                updatePageEmbeddingTask = UpdatePageEmbeddings();
            }

            if (updatePageEmbeddingTask != null && !updatePageEmbeddingTask.IsCompleted)
                try
                {
                    await updatePageEmbeddingTask;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Exception while waiting for page embedding task to complete during shutdown");
                }

            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }
    }

    async Task UpdatePageEmbeddings()
    {
        var pagesPerRequest = 25;
        IEnumerable<Content> updateList = await _contentRepo
            .GetContentsAsync(ct => ct.NeedsEmbedding && !string.IsNullOrEmpty(ct.Text), pagesPerRequest, currentPage);
        var enumerable = updateList.ToArray();
        _logger.LogDebug("Found {ContentCount} pages that need new embeddings", enumerable.Length);

        foreach (var content in enumerable)
        {
            _logger.LogDebug("Fixing Text...");
            var wordArray = StripInvalidWords(content.Text);
            var wordChunks = ChunkWords(wordArray);

            content.Embeddings = await _aiService.EmbedDocumentAsync(wordChunks);
            foreach (var emb in content.Embeddings)
            {
                emb.EmbeddingHash = ComputeContentHash(emb.RawText);
                emb.Content = content;
            }

            _logger.LogDebug($"Hash generated: {content.ContentHash}");
            content.ContentHash = ComputeContentHash(content.Text);
        }

        currentPage += updateList.Count();
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
            _logger.LogDebug($"Chunk {i}: {string.Join(", ", chunks[i])}");
        }

        return chunks;
    }

    private string ComputeContentHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = _hasher.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}