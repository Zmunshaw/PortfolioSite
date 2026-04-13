using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class VideoRating : ValueObject
{
    public float Value { get; }

    public VideoRating(float value)
    {
        Guard.AgainstOutOfRange(value, 0.0f, 5.0f);
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("F1");
}
