using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;
using Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public class NewsDocument : Entity<Guid>
{
    public Url PageLocation { get; private set; } = null!;
    public Publication Publication { get; private set; } = null!;
    public DateTime PublicationDate { get; private set; }
    public string Headline { get; private set; } = null!;
    public IndexState State { get; private set; } = IndexState.Initial;
    public EmbeddingSet? Embeddings { get; private set; }

    private NewsDocument() { }

    internal NewsDocument(Url pageLocation, Publication publication, DateTime publicationDate, string headline)
    {
        Guard.AgainstNull(pageLocation);
        Guard.AgainstNull(publication);
        Guard.AgainstNullOrWhiteSpace(headline);

        Id = Guid.NewGuid();
        PageLocation = pageLocation;
        Publication = publication;
        PublicationDate = publicationDate;
        Headline = headline;
        State = State.WithIndexed(DateTime.UtcNow);
    }

    internal void SetEmbeddings(EmbeddingSet embeddings)
    {
        Guard.AgainstNull(embeddings);
        Embeddings = embeddings;
        SetUpdated();
    }
}
