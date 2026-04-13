using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Roots.Sitemap.ValueObjects;

public sealed class ImageMedia : ValueObject
{
    public SitemapLocation Location { get; }

    public ImageMedia(string location)
    {
        Guard.AgainstNullOrWhiteSpace(location);
        Location = new SitemapLocation(location);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Location;
    }
}
