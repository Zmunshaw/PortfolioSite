namespace SiteBackend.Repositories.SearchEngine;

public interface IDictionaryRepo
{
    Task AddWords(string[] words);

    bool RepoContains(string key);
}