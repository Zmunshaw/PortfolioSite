using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class VideoDuration : ValueObject
{
    public int Seconds { get; }

    public VideoDuration(int seconds)
    {
        Guard.AgainstOutOfRange(seconds, 1, 28800);
        Seconds = seconds;
    }

    public TimeSpan ToTimeSpan() => TimeSpan.FromSeconds(Seconds);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Seconds;
    }

    public override string ToString() => ToTimeSpan().ToString();
}
