using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class UrlPriority : ValueObject
{
    public static UrlPriority Default => new(0.5f);

    public float Value { get; }

    public UrlPriority(float value)
    {
        Guard.AgainstOutOfRange(value, 0.0f, 1.0f);
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
