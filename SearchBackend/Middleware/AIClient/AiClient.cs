using System.ClientModel;
using System;
using OpenAI;
using OpenAI.Embeddings;

namespace SiteBackend.Middleware.AIClient;

public partial class AiClient : IAiClient
{
    private readonly ILogger<AiClient> _logger;
    private readonly EmbeddingClient _denseClient;
    private readonly EmbeddingClient _sparseClient;
    
    // MARKED: for #30
    // Client
    OpenAIClientOptions _options;
    string _endpoint = Environment.GetEnvironmentVariable("AI_ENDPOINT") ?? "http://local-ai:1122/v1";
    string _apiKey = Environment.GetEnvironmentVariable("AI_API_KEY") ?? "not-needed";
    // MARKED: for #30
    // Sparse Settings
    private readonly string _defaultSparseEmbeddingModel = Environment.GetEnvironmentVariable("AI_DENSE_MODEL") 
                                                           ?? "text-embedding-splade-v3";
    // MARKED: for #30
    // Dense Settings
    private readonly string _defaultDenseEmbeddingModel = Environment.GetEnvironmentVariable("AI_SPARSE_MODEL") 
                                                          ??"text-embedding-granite-embedding-278m-multilingual";
    
    public AiClient(ILogger<AiClient> logger)
    {
        _logger = logger;
        ValidateConfigs();
        
        Uri aiHost = new(_endpoint);
        _options = new OpenAIClientOptions
        {
            Endpoint = aiHost
        };
        var aiHostCredential = new ApiKeyCredential(_apiKey);

        _denseClient = new EmbeddingClient(
            _defaultDenseEmbeddingModel,
            aiHostCredential,
            _options
        );
        _sparseClient = new EmbeddingClient(
            _defaultSparseEmbeddingModel,
            aiHostCredential,
            _options
        );
    }

    private void ValidateConfigs()
    {
        _logger.LogDebug("Validating configs");
        ValidateEndpoint(_endpoint);
        _logger.LogDebug("Config Validation complete...");
    }

    private void ValidateEndpoint(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            throw new ArgumentNullException(nameof(endpoint));
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            throw new ArgumentException($"Invalid endpoint '{endpoint}'");
    }

    private void ValidateApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentNullException(nameof(apiKey));
    }
}