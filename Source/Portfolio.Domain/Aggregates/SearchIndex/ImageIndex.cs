using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Errors;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Errors;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.SearchIndex.Events;
using Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public class ImageIndex : AggregateRoot<Guid>
{
    public static class Errors
    {
        public static Error DuplicateImage(string imageUrl) =>
            new("IMAGE_INDEX_DUPLICATE", $"Image '{imageUrl}' is already indexed.");

        public static Error DocumentNotFound(Guid documentId) =>
            new("IMAGE_INDEX_DOC_NOT_FOUND", $"Image document '{documentId}' not found.");
    }

    private readonly List<ImageDocument> _documents = [];

    public Url Host { get; private set; } = null!;
    public IReadOnlyList<ImageDocument> Documents => _documents.AsReadOnly();

    private ImageIndex() { }

    public static ImageIndex Create(string host)
    {
        return new ImageIndex
        {
            Id = Guid.NewGuid(),
            Host = new Url(host)
        };
    }

    public ImageDocument IndexImage(string pageUrl, string imageUrl, string? altText = null, ImageDimensions? dimensions = null)
    {
        var imgLoc = new Url(imageUrl);

        if (_documents.Any(d => d.ImageLocation == imgLoc))
            throw new DomainLayerException(Errors.DuplicateImage(imageUrl));

        var doc = new ImageDocument(new Url(pageUrl), imgLoc, altText, dimensions);
        _documents.Add(doc);
        SetUpdated();

        AddDomainEvent(
            ImageIndexedEvent.Create(Id, doc.Id, pageUrl, imageUrl));

        return doc;
    }

    public void UpdateEmbeddings(Guid documentId, EmbeddingSet embeddings)
    {
        Guard.AgainstNull(embeddings);
        var doc = FindDocument(documentId);
        doc.SetEmbeddings(embeddings);
        SetUpdated();
    }

    public void RemoveImage(Guid documentId)
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

    private ImageDocument FindDocument(Guid documentId)
        => _documents.FirstOrDefault(d => d.Id == documentId)
           ?? throw new DomainLayerException(Errors.DocumentNotFound(documentId));
}
