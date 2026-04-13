using Portfolio.Common.Seedwork.Aggregates;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class TagSet : ValueObject
{
    private const int MaxTags = 32;

    public IReadOnlyList<string> Values { get; }

    public TagSet(IEnumerable<string>? tags = null)
    {
        var list = tags?
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        if (list.Count > MaxTags)
            throw new ArgumentException($"Cannot have more than {MaxTags} tags.", nameof(tags));

        Values = list.AsReadOnly();
    }

    public static TagSet Empty => new();

    public int Count => Values.Count;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var tag in Values)
            yield return tag;
    }
}
