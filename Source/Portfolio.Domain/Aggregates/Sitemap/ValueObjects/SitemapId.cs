using Portfolio.Common.Seedwork.Aggregates;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class SitemapId : ValueObject
{
    public Guid Value { get; }

    public SitemapId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("SitemapId cannot be empty.", nameof(value));

        Value = value;
    }

    public static SitemapId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
