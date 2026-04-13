using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public class VideoDocument : Entity<Guid>
{
    public Url PageLocation { get; private set; } = null!;
    public VideoMetadata Metadata { get; private set; } = null!;
    public IndexState State { get; private set; } = IndexState.Initial;
    public EmbeddingSet? Embeddings { get; private set; }

    private VideoDocument() { }

    internal VideoDocument(Url pageLocation, VideoMetadata metadata)
    {
        Guard.AgainstNull(pageLocation);
        Guard.AgainstNull(metadata);

        Id = Guid.NewGuid();
        PageLocation = pageLocation;
        Metadata = metadata;
        State = State.WithIndexed(DateTime.UtcNow);
    }

    internal void SetEmbeddings(EmbeddingSet embeddings)
    {
        Guard.AgainstNull(embeddings);
        Embeddings = embeddings;
        SetUpdated();
    }
}
