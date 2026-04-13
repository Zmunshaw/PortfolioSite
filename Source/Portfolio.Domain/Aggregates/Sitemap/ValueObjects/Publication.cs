using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class Publication : ValueObject
{
    public string Name { get; }
    public LanguageCode Language { get; }

    public Publication(string name, string language)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name);
        Language = new LanguageCode(language);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Language;
    }
}
