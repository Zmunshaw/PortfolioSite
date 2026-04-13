using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class VideoRestriction : ValueObject
{
    public string Relationship { get; }
    public IReadOnlyList<string> Countries { get; }

    public VideoRestriction(string relationship, IEnumerable<string> countries)
    {
        Relationship = Guard.AgainstNullOrWhiteSpace(relationship);

        if (relationship is not ("allow" or "deny"))
            throw new ArgumentException("Relationship must be 'allow' or 'deny'.", nameof(relationship));

        var list = countries?.ToList() ?? [];
        Guard.AgainstEmptyCollection<string>(list.AsReadOnly());
        Countries = list.AsReadOnly();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Relationship;
        foreach (var country in Countries)
            yield return country;
    }
}
