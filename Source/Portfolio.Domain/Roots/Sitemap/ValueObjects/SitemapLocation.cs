using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Roots.Sitemap.ValueObjects;

public sealed class SitemapLocation : ValueObject
{
    public string Value { get; }

    public SitemapLocation(string uri)
    {
        Guard.AgainstNullOrWhiteSpace(uri);

        if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
            throw new ArgumentException($"'{uri}' is not a valid absolute URI.", nameof(uri));

        Value = uri;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
