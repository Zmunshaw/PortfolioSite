using Portfolio.Common.Seedwork.Aggregates;

namespace Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

public sealed class ImageDimensions : ValueObject
{
    public int Width { get; }
    public int Height { get; }

    public ImageDimensions(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Width must be positive.", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Height must be positive.", nameof(height));

        Width = width;
        Height = height;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Width;
        yield return Height;
    }
}
