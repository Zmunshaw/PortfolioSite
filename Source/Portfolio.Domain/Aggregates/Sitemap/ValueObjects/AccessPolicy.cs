using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class AccessPolicy : ValueObject
{
    public static AccessPolicy Allow(IEnumerable<string> values) => new("allow", values);
    public static AccessPolicy Deny(IEnumerable<string> values) => new("deny", values);

    public string Relationship { get; }
    public IReadOnlyList<string> Values { get; }

    private AccessPolicy(string relationship, IEnumerable<string> values)
    {
        Relationship = Guard.AgainstNullOrWhiteSpace(relationship);

        if (relationship is not ("allow" or "deny"))
            throw new ArgumentException("Relationship must be 'allow' or 'deny'.", nameof(relationship));

        var list = values?.ToList() ?? [];
        Guard.AgainstEmptyCollection<string>(list.AsReadOnly());
        Values = list.AsReadOnly();
    }

    public bool IsAllowed => Relationship == "allow";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Relationship;
        foreach (var value in Values)
            yield return value;
    }
}
