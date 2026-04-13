using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class VideoPlatform : ValueObject
{
    private static readonly HashSet<string> ValidPlatforms = new(StringComparer.OrdinalIgnoreCase)
        { "web", "mobile", "tv" };

    public string Relationship { get; }
    public IReadOnlyList<string> Platforms { get; }

    public VideoPlatform(string relationship, IEnumerable<string> platforms)
    {
        Relationship = Guard.AgainstNullOrWhiteSpace(relationship);

        if (relationship is not ("allow" or "deny"))
            throw new ArgumentException("Relationship must be 'allow' or 'deny'.", nameof(relationship));

        var list = platforms?.ToList() ?? [];
        Guard.AgainstEmptyCollection<string>(list.AsReadOnly());

        var invalid = list.Where(p => !ValidPlatforms.Contains(p)).ToList();
        if (invalid.Count > 0)
            throw new ArgumentException(
                $"Invalid platform values: {string.Join(", ", invalid)}. Must be web, mobile, or tv.",
                nameof(platforms));

        Platforms = list.AsReadOnly();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Relationship;
        foreach (var platform in Platforms)
            yield return platform;
    }
}
