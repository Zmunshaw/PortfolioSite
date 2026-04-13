using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Errors;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Errors;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.SearchIndex.Events;
using Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;
using Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public class NewsIndex : AggregateRoot<Guid>
{
    public static class Errors
    {
        public static Error DuplicateNews(string pageUrl, string headline) =>
            new("NEWS_INDEX_DUPLICATE", $"News '{headline}' on page '{pageUrl}' is already indexed.");

        public static Error DocumentNotFound(Guid documentId) =>
            new("NEWS_INDEX_DOC_NOT_FOUND", $"News document '{documentId}' not found.");
    }

    private readonly List<NewsDocument> _documents = [];

    public Url Host { get; private set; } = null!;
    public IReadOnlyList<NewsDocument> Documents => _documents.AsReadOnly();

    private NewsIndex() { }

    public static NewsIndex Create(string host)
    {
        return new NewsIndex
        {
            Id = Guid.NewGuid(),
            Host = new Url(host)
        };
    }

    public NewsDocument IndexNews(string pageUrl, Publication publication, DateTime publicationDate, string headline)
    {
        Guard.AgainstNull(publication);
        Guard.AgainstNullOrWhiteSpace(headline);

        var pageLoc = new Url(pageUrl);

        if (_documents.Any(d => d.PageLocation == pageLoc && d.Headline == headline))
            throw new DomainLayerException(Errors.DuplicateNews(pageUrl, headline));

        var doc = new NewsDocument(pageLoc, publication, publicationDate, headline);
        _documents.Add(doc);
        SetUpdated();

        AddDomainEvent(
            NewsIndexedEvent.Create(Id, doc.Id, pageUrl));

        return doc;
    }

    public void UpdateEmbeddings(Guid documentId, EmbeddingSet embeddings)
    {
        Guard.AgainstNull(embeddings);
        var doc = FindDocument(documentId);
        doc.SetEmbeddings(embeddings);
        SetUpdated();
    }

    public void RemoveNews(Guid documentId)
    {
        var doc = FindDocument(documentId);
        _documents.Remove(doc);
        SetUpdated();
    }

    private NewsDocument FindDocument(Guid documentId)
        => _documents.FirstOrDefault(d => d.Id == documentId)
           ?? throw new DomainLayerException(Errors.DocumentNotFound(documentId));
}
