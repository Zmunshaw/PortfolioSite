using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Roots.Sitemap.ValueObjects;

public sealed class AlternateLink : ValueObject
{
    public string Hreflang { get; }
    public SitemapLocation Href { get; }

    public AlternateLink(string hreflang, string href)
    {
        Hreflang = Guard.AgainstNullOrWhiteSpace(hreflang);
        Href = new SitemapLocation(href);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hreflang;
        yield return Href;
    }
}
