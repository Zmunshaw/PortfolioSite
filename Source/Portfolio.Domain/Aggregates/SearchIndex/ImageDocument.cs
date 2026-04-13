using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public class ImageDocument : Entity<Guid>
{
    public Url PageLocation { get; private set; } = null!;
    public Url ImageLocation { get; private set; } = null!;
    public string? AltText { get; private set; }
    public ImageDimensions? Dimensions { get; private set; }
    public IndexState State { get; private set; } = IndexState.Initial;

    private ImageDocument() { }

    internal ImageDocument(Url pageLocation, Url imageLocation, string? altText = null, ImageDimensions? dimensions = null)
    {
        Guard.AgainstNull(pageLocation);
        Guard.AgainstNull(imageLocation);

        Id = Guid.NewGuid();
        PageLocation = pageLocation;
        ImageLocation = imageLocation;
        AltText = altText;
        Dimensions = dimensions;
        State = State.WithIndexed(DateTime.UtcNow);
    }
}
