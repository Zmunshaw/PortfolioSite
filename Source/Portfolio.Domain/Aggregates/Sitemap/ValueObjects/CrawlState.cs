using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Domain.Aggregates.Sitemap.Enums;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class CrawlState : ValueObject
{
    public static CrawlState Initial => new(null, null, 0);

    public DateTime? LastAttempt { get; }
    public DateTime? LastSuccess { get; }
    public int Attempts { get; }

    private CrawlState(DateTime? lastAttempt, DateTime? lastSuccess, int attempts)
    {
        LastAttempt = lastAttempt;
        LastSuccess = lastSuccess;
        Attempts = attempts;
    }

    public CrawlState WithAttempt(DateTime at)
        => new(at, LastSuccess, Attempts + 1);

    public CrawlState WithSuccess(DateTime at)
        => new(at, at, Attempts + 1);

    public bool IsDue(DateTime asOf, ChangeFrequency? changeFrequency)
    {
        if (LastSuccess is null) return true;

        var interval = changeFrequency switch
        {
            _ when changeFrequency == ChangeFrequency.Always  => TimeSpan.Zero,
            _ when changeFrequency == ChangeFrequency.Hourly  => TimeSpan.FromHours(1),
            _ when changeFrequency == ChangeFrequency.Daily   => TimeSpan.FromDays(1),
            _ when changeFrequency == ChangeFrequency.Weekly  => TimeSpan.FromDays(7),
            _ when changeFrequency == ChangeFrequency.Monthly => TimeSpan.FromDays(30),
            _ when changeFrequency == ChangeFrequency.Yearly  => TimeSpan.FromDays(365),
            _ when changeFrequency == ChangeFrequency.Never   => TimeSpan.MaxValue,
            _ => TimeSpan.FromDays(1)
        };

        return asOf - LastSuccess.Value >= interval;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LastAttempt;
        yield return LastSuccess;
        yield return Attempts;
    }
}
