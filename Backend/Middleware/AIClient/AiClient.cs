using System.ClientModel;
using OpenAI;
using OpenAI.Embeddings;

namespace SiteBackend.Middleware.AIClient;

public partial class AiClient : IAiClient
{
    private readonly ILogger<AiClient> _logger;

    public AiClient(ILogger<AiClient> logger)
    {
        _logger = logger;
        _logger.LogInformation("Starting AI Client");
        var aiHost = Environment.GetEnvironmentVariable("AI_HOST") ?? "http://localai";
        var aiPort = Environment.GetEnvironmentVariable("AI_PORT") ?? "8080";
        Uri lmStudioUri = new($"{aiHost}:{aiPort}/v1");
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = lmStudioUri
        };
        var dummyCredential = new ApiKeyCredential("not-needed");

        _denseClient = new EmbeddingClient(
            _defaultDenseEmbeddingModel,
            dummyCredential,
            clientOptions
        );
        _sparseClient = new EmbeddingClient(
            _defaultSparseEmbeddingModel,
            dummyCredential,
            clientOptions
        );
    }

    #region Client

    private readonly EmbeddingClient _denseClient;
    private readonly EmbeddingClient _sparseClient;

    // TODO: Move to a settings file or smth
    private readonly string _defaultDenseEmbeddingModel = "granite-embedding-125m-english";
    private readonly string _defaultSparseEmbeddingModel = "splade-model";

    #endregion
}