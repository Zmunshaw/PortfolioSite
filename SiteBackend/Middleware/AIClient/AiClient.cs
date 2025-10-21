using OllamaSharp;
using OllamaSharp.Models;

namespace SiteBackend.Middleware.AIClient;

public partial class AiClient : IAiClient
{
    #region Injected
    private readonly ILogger<AiClient> _logger;
    #endregion
    
    #region Client
    private readonly OllamaApiClient _ollama;
    private readonly string _defaultEmbeddingModel = "embeddinggemma:latest";
    private readonly string _defaultCompletionModel = "huihui_ai/deepseek-r1-abliterated:8b";
    #endregion
    
    public AiClient()
    {
        // TODO: appsettings.json this
        Uri ollamaHost = new("http://localhost:7869");
        _ollama = new OllamaApiClient(ollamaHost);
        _ = InitModel(_defaultEmbeddingModel);
        _ollama.SelectedModel = _defaultEmbeddingModel;
    }

    /// <summary>
    /// Calls on AI server to download desired model.
    /// </summary>
    /// <param name="model">Model Name e.g. "gpt-oss:latest"</param>
    public async Task PullModel(string model)
    {
        _logger.LogDebug("Downloading model {model}....", model);
        _ollama.PullModelAsync(model);
    }
    
    public async Task<bool> SetModel(int model)
    {
        List<Model> availableModels = await GetModels();
        
        _logger.LogDebug("Selecting model {model}", model);
        if (availableModels.Count > model) return false;
        
        _ollama.SelectedModel = availableModels[model].Name;
        
        return true;

    }

    public async Task<bool> SetModel(string model)
    {
        List<Model> availableModels = await GetModels();
        _logger.LogDebug("Selected model: " + model);
        if (availableModels.Any(mdl => mdl.Name == model))
        {
            _ollama.SelectedModel = model;
            _logger.LogDebug($"Model {model} was successfully set.");
            return true;
        }
        
        _logger.LogWarning($"Failed to set model {model}.");
        return false;
    }

    async Task<List<Model>> GetModels()
    {
        _logger.LogDebug("Getting models...");
        return _ollama.ListLocalModelsAsync().Result.ToList();
    }
}