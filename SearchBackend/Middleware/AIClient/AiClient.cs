using System.ClientModel;
using System;
using OpenAI;
using OpenAI.Embeddings;

namespace SiteBackend.Middleware.AIClient;

public partial class AiClient : IAiClient
{
    #region Injected

    private readonly ILogger<AiClient> _logger;

    #endregion

    public AiClient(ILogger<AiClient> logger)
    {
        _logger = logger;
        // Configure from environment with sensible defaults
        var endpoint = Environment.GetEnvironmentVariable("AI_ENDPOINT") ?? "http://local-ai:1122/v1";
        var denseModel = Environment.GetEnvironmentVariable("AI_DENSE_MODEL") ?? _defaultDenseEmbeddingModel;
        var sparseModel = Environment.GetEnvironmentVariable("AI_SPARSE_MODEL") ?? _defaultSparseEmbeddingModel;

        Uri lmStudioUri = new(endpoint);
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = lmStudioUri
        };
        var dummyCredential = new ApiKeyCredential("not-needed");

        _denseClient = new EmbeddingClient(
            denseModel,
            dummyCredential,
            clientOptions
        );
        _sparseClient = new EmbeddingClient(
            sparseModel,
            dummyCredential,
            clientOptions
        );
    }

    #region Client

    private readonly EmbeddingClient _denseClient;
    private readonly EmbeddingClient _sparseClient;

    // TODO: Move to a settings file or smth
    private readonly string _defaultDenseEmbeddingModel = "text-embedding-granite-embedding-278m-multilingual";
    private readonly string _defaultSparseEmbeddingModel = "text-embedding-splade-v3";

    #endregion
}