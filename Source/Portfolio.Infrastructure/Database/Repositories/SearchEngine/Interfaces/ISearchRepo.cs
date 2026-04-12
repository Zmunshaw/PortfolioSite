namespace Portfolio.Infrastructure.Database.Repositories.SearchEngine.Interfaces;

public interface ISearchRepo
{
    Task<IEnumerable<DTOSearchResult>> GetSearchResults(DTOSearchRequest request);

    Task<IEnumerable<Word>> GetSimilarWords(SparseVector wordVector, int take = 25, int skip = 0,
        double? maxDistance = null);
}