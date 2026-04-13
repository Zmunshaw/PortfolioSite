using Portfolio.Common.Seedwork.Aggregates;

namespace Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

public sealed class SearchIndexId : ValueObject
{
    public Guid Value { get; }

    public SearchIndexId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("SearchIndexId cannot be empty.", nameof(value));

        Value = value;
    }

    public static SearchIndexId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
