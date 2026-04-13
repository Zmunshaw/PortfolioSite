using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public class PageDocument : Entity<Guid>
{
    public Url Location { get; private set; } = null!;
    public PageContent Content { get; private set; } = null!;
    public IndexState State { get; private set; } = IndexState.Initial;
    public EmbeddingSet? Embeddings { get; private set; }

    private PageDocument() { }

    internal PageDocument(Url location, PageContent content)
    {
        Guard.AgainstNull(location);
        Guard.AgainstNull(content);

        Id = Guid.NewGuid();
        Location = location;
        Content = content;
        State = State.WithIndexed(DateTime.UtcNow);
    }

    internal void UpdateContent(PageContent content)
    {
        Guard.AgainstNull(content);
        Content = content;
        State = State.WithIndexed(DateTime.UtcNow);
        SetUpdated();
    }

    internal void SetEmbeddings(EmbeddingSet embeddings)
    {
        Guard.AgainstNull(embeddings);
        Embeddings = embeddings;
        SetUpdated();
    }
}
