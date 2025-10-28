using System.ClientModel;
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
        // TODO: Move to a settings file or smth
        Uri lmStudioUri = new("http://172.17.0.1:1122/v1");
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
    private readonly string _defaultDenseEmbeddingModel = "text-embedding-granite-embedding-278m-multilingual";
    private readonly string _defaultSparseEmbeddingModel = "text-embedding-splade-v3";

    #endregion
}