using Portfolio.Common.Seedwork.Aggregates;

namespace Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

public sealed class IndexState : ValueObject
{
    public static IndexState Initial => new(null, 0);

    public DateTime? LastIndexed { get; }
    public int IndexVersion { get; }

    private IndexState(DateTime? lastIndexed, int indexVersion)
    {
        LastIndexed = lastIndexed;
        IndexVersion = indexVersion;
    }

    public IndexState WithIndexed(DateTime at)
        => new(at, IndexVersion + 1);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LastIndexed;
        yield return IndexVersion;
    }
}
