using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Roots.Sitemap.ValueObjects;

public sealed class NewsMedia : ValueObject
{
    public Publication Publication { get; }
    public DateTime PublicationDate { get; }
    public string Title { get; }

    public NewsMedia(Publication publication, DateTime publicationDate, string title)
    {
        Publication = Guard.AgainstNull(publication);
        PublicationDate = publicationDate;
        Title = Guard.AgainstNullOrWhiteSpace(title);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Publication;
        yield return PublicationDate;
        yield return Title;
    }
}
