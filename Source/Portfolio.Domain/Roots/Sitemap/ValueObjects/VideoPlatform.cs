using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Roots.Sitemap.ValueObjects;

public sealed class VideoPlatform : ValueObject
{
    public string Relationship { get; }
    public IReadOnlyList<string> Platforms { get; }

    public VideoPlatform(string relationship, IEnumerable<string> platforms)
    {
        Relationship = Guard.AgainstNullOrWhiteSpace(relationship);

        if (relationship is not ("allow" or "deny"))
            throw new ArgumentException("Relationship must be 'allow' or 'deny'.", nameof(relationship));

        var list = platforms?.ToList() ?? [];
        Guard.AgainstEmptyCollection<string>(list.AsReadOnly());
        Platforms = list.AsReadOnly();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Relationship;
        foreach (var platform in Platforms)
            yield return platform;
    }
}
