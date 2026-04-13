using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Roots.Sitemap.ValueObjects;

public sealed class Publication : ValueObject
{
    public string Name { get; }
    public string Language { get; }

    public Publication(string name, string language)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name);
        Language = Guard.AgainstNullOrWhiteSpace(language);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Language;
    }
}
