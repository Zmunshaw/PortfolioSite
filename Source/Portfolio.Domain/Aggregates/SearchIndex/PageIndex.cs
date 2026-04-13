using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Errors;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Errors;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.SearchIndex.Events;
using Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public class PageIndex : AggregateRoot<Guid>
{
    public static class Errors
    {
        public static Error DuplicateDocument(string url) =>
            new("PAGE_INDEX_DUPLICATE", $"Page '{url}' is already indexed.");

        public static Error DocumentNotFound(Guid documentId) =>
            new("PAGE_INDEX_DOC_NOT_FOUND", $"Page document '{documentId}' not found.");

        public static Error StaleContentHash(Guid documentId) =>
            new("PAGE_INDEX_STALE_HASH", $"Content hash for document '{documentId}' has not changed.");
    }

    private readonly List<PageDocument> _documents = [];

    public Url Host { get; private set; } = null!;
    public IReadOnlyList<PageDocument> Documents => _documents.AsReadOnly();

    private PageIndex() { }

    public static PageIndex Create(string host)
    {
        return new PageIndex
        {
            Id = Guid.NewGuid(),
            Host = new Url(host)
        };
    }

    public PageDocument IndexPage(string url, string title, string contentHash, string? description = null, string? snippet = null)
    {
        var loc = new Url(url);

        if (_documents.Any(d => d.Location == loc))
            throw new DomainLayerException(Errors.DuplicateDocument(url));

        var content = new PageContent(title, contentHash, description, snippet);
        var doc = new PageDocument(loc, content);
        _documents.Add(doc);
        SetUpdated();

        AddDomainEvent(
            PageIndexedEvent.Create(Id, doc.Id, url));

        return doc;
    }

    public void ReindexPage(Guid documentId, string title, string contentHash, string? description = null, string? snippet = null)
    {
        var doc = FindDocument(documentId);

        if (doc.Content.ContentHash == contentHash)
            throw new DomainLayerException(Errors.StaleContentHash(documentId));

        var content = new PageContent(title, contentHash, description, snippet);
        doc.UpdateContent(content);
        SetUpdated();
    }

    public void UpdateEmbeddings(Guid documentId, EmbeddingSet embeddings)
    {
        Guard.AgainstNull(embeddings);
        var doc = FindDocument(documentId);
        doc.SetEmbeddings(embeddings);
        SetUpdated();
    }

    public void RemovePage(Guid documentId)
    {
        var doc = FindDocument(documentId);
        _documents.Remove(doc);
        SetUpdated();

        AddDomainEvent(
            PageRemovedEvent.Create(Id, documentId));
    }

    private PageDocument FindDocument(Guid documentId)
        => _documents.FirstOrDefault(d => d.Id == documentId)
           ?? throw new DomainLayerException(Errors.DocumentNotFound(documentId));
}
