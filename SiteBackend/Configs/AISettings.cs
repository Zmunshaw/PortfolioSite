namespace SiteBackend.Configs;

public class AISettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string DefaultEmbbeddingsModel { get; set; }
    public string DefaultCompletionsModel { get; set; }
}