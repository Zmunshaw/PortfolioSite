using System.Security.Cryptography;
using System.Text;
using SiteBackend.Models.SearchEngine.Index;
using SiteBackend.Repositories.SearchEngine;
using SiteBackend.Services;

namespace SiteBackend.Singletons;

public class EmbeddingManager : BackgroundService
{
    // TODO: Move to a settings file or smth
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
            if (updatePageEmbeddingTask == null || updatePageEmbeddingTask.IsCompleted)
            {
                if (updatePageEmbeddingTask == null || updatePageEmbeddingTask.IsCompletedSuccessfully)
                    updatePageEmbeddingTask = UpdatePageEmbeddings();
                else
                    _logger.LogWarning(updatePageEmbeddingTask.Exception?.ToString());
                await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
            }
        }
    }

    #region Content Embedding

    async Task UpdatePageEmbeddings()
    {
        var pagesPerRequest = 25;
        IEnumerable<Content> updateList = await _contentRepo
            .GetContentsAsync(ct => ct.Embeddings.Count == 0 && !string.IsNullOrEmpty(ct.Text),
                pagesPerRequest, currentPage);
        var enumerable = updateList.ToArray();
        if (enumerable.Length != 0)
            _logger.LogDebug("Found {ContentCount} pages that need new embeddings", enumerable.Length);

        foreach (var content in enumerable)
        {
            //_logger.LogDebug("Fixing Text...");
            var wordArray = StripInvalidWords(content.Text);
            var wordChunks = ChunkWords(wordArray);

            content.Embeddings = await _aiService.EmbedDocumentAsync(wordChunks);

            foreach (var emb in content.Embeddings)
            {
                emb.Content = content;
            }

            //_logger.LogDebug($"Hash generated: {content.ContentHash}");
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
        }

        return chunks;
    }

    private string ComputeContentHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = _hasher.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion
}