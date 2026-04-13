using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Domain.Aggregates.Shared;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class AlternateLink : ValueObject
{
    public LanguageCode Hreflang { get; }
    public Url Href { get; }

    public AlternateLink(string hreflang, string href)
    {
        Hreflang = new LanguageCode(hreflang);
        Href = new Url(href);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hreflang;
        yield return Href;
    }
}
