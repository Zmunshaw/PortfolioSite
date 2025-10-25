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
                updatePageEmbeddingTask = Task.Run(async () => await UpdatePageEmbeddings());
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

                updatePageEmbeddingTask = Task.Run(() => UpdatePageEmbeddings());
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }
    }

    async Task UpdatePageEmbeddings()
    {
        IEnumerable<Content> updateList = await _contentRepo
            .GetContentsAsync(ct => ct.NeedsEmbedding && !string.IsNullOrEmpty(ct.Text), 50);
        var enumerable = updateList as Content[] ?? updateList.ToArray();

        _logger.LogDebug("Found {ContentCount} pages that need new embeddings", enumerable.Length);

        foreach (var content in enumerable)
        {
            _logger.LogDebug("Fixing Text...");
            var wordArray = StripInvalidWords(content.Text);
            var wordChunks = ChunkWords(wordArray);

            content.Embeddings = await _aiService.EmbedDocumentAsync(content.Title, wordChunks);
            foreach (var emb in content.Embeddings)
                emb.EmbeddingHash = ComputeContentHash(emb.RawText);

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
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = _hasher.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}