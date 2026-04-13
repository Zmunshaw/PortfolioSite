using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class UrlLimit : ValueObject
{
    public int Value { get; }

    public UrlLimit(int value)
    {
        Guard.AgainstOutOfRange(value, 1, int.MaxValue);
        Value = value;
    }

    public bool IsExceededBy(int count) => count >= Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
