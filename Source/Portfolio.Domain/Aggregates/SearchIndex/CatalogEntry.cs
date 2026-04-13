using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.SearchIndex.Enums;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public class CatalogEntry : Entity<Guid>
{
    public Url Location { get; private set; } = null!;
    public ContentType ContentType { get; private set; } = null!;
    public DateTime RegisteredAt { get; private set; }

    private CatalogEntry() { }

    internal CatalogEntry(Url location, ContentType contentType)
    {
        Guard.AgainstNull(location);
        Guard.AgainstNull(contentType);

        Id = Guid.NewGuid();
        Location = location;
        ContentType = contentType;
        RegisteredAt = DateTime.UtcNow;
    }
}
