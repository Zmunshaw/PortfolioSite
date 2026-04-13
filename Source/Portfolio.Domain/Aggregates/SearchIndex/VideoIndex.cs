using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Errors;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Errors;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.SearchIndex.Events;
using Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public class VideoIndex : AggregateRoot<Guid>
{
    public static class Errors
    {
        public static Error DuplicateVideo(string pageUrl, string title) =>
            new("VIDEO_INDEX_DUPLICATE", $"Video '{title}' on page '{pageUrl}' is already indexed.");

        public static Error DocumentNotFound(Guid documentId) =>
            new("VIDEO_INDEX_DOC_NOT_FOUND", $"Video document '{documentId}' not found.");
    }

    private readonly List<VideoDocument> _documents = [];

    public Url Host { get; private set; } = null!;
    public IReadOnlyList<VideoDocument> Documents => _documents.AsReadOnly();

    private VideoIndex() { }

    public static VideoIndex Create(string host)
    {
        return new VideoIndex
        {
            Id = Guid.NewGuid(),
            Host = new Url(host)
        };
    }

    public VideoDocument IndexVideo(string pageUrl, VideoMetadata metadata)
    {
        Guard.AgainstNull(metadata);

        var pageLoc = new Url(pageUrl);

        if (_documents.Any(d => d.PageLocation == pageLoc && d.Metadata.Title == metadata.Title))
            throw new DomainLayerException(Errors.DuplicateVideo(pageUrl, metadata.Title));

        var doc = new VideoDocument(pageLoc, metadata);
        _documents.Add(doc);
        SetUpdated();

        AddDomainEvent(
            VideoIndexedEvent.Create(Id, doc.Id, pageUrl));

        return doc;
    }

    public void UpdateEmbeddings(Guid documentId, EmbeddingSet embeddings)
    {
        Guard.AgainstNull(embeddings);
        var doc = FindDocument(documentId);
        doc.SetEmbeddings(embeddings);
        SetUpdated();
    }

    public void RemoveVideo(Guid documentId)
    {
        var doc = FindDocument(documentId);
        _documents.Remove(doc);
        SetUpdated();
    }

    public void RemoveAllForPage(string pageUrl)
    {
        var loc = new Url(pageUrl);
        _documents.RemoveAll(d => d.PageLocation == loc);
        SetUpdated();
    }

    private VideoDocument FindDocument(Guid documentId)
        => _documents.FirstOrDefault(d => d.Id == documentId)
           ?? throw new DomainLayerException(Errors.DocumentNotFound(documentId));
}
