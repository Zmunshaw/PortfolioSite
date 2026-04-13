using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Domain.Aggregates.Shared;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class ImageMedia : ValueObject
{
    public Url Location { get; }

    public ImageMedia(string location)
    {
        Location = new Url(location);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Location;
    }
}
