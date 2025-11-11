using SiteBackend.Repositories.SearchEngine;
using SiteBackend.Services;

namespace SiteBackend.Singletons;

public class KeywordService : IKeywordService
{
    private readonly IAIService _aiService;
    private readonly IContentRepo _contentRepo;
    private readonly IDictionaryRepo _dictionaryRepo;
    private readonly ILogger<EmbeddingManager> _logger;

    public KeywordService(IAIService aiService, IContentRepo contentRepo,
        IDictionaryRepo dictRepo, ILogger<EmbeddingManager> logger)
    {
        _aiService = aiService;
        _contentRepo = contentRepo;
        _dictionaryRepo = dictRepo;
        _logger = logger;
    }
}