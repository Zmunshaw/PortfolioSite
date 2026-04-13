using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Errors;
using Portfolio.Common.Seedwork.Guards;
using Portfolio.Domain.Errors;
using Portfolio.Domain.Aggregates.Shared;
using Portfolio.Domain.Aggregates.SearchIndex.Enums;
using Portfolio.Domain.Aggregates.SearchIndex.Events;

namespace Portfolio.Domain.Aggregates.SearchIndex;

public class IndexCatalog : AggregateRoot<Guid>
{
    public static class Errors
    {
        public static Error DuplicateEntry(string url, ContentType type) =>
            new("CATALOG_DUPLICATE_ENTRY", $"URL '{url}' is already registered as '{type.Name}'.");

        public static Error EntryNotFound(Guid entryId) =>
            new("CATALOG_ENTRY_NOT_FOUND", $"Catalog entry '{entryId}' not found.");
    }

    private readonly List<CatalogEntry> _entries = [];

    public Url Host { get; private set; } = null!;
    public IReadOnlyList<CatalogEntry> Entries => _entries.AsReadOnly();

    private IndexCatalog() { }

    public static IndexCatalog Create(string host)
    {
        return new IndexCatalog
        {
            Id = Guid.NewGuid(),
            Host = new Url(host)
        };
    }

    public CatalogEntry Register(string url, ContentType contentType)
    {
        Guard.AgainstNull(contentType);

        var loc = new Url(url);

        if (_entries.Any(e => e.Location == loc && e.ContentType == contentType))
            throw new DomainLayerException(Errors.DuplicateEntry(url, contentType));

        var entry = new CatalogEntry(loc, contentType);
        _entries.Add(entry);
        SetUpdated();

        AddDomainEvent(
            UrlRegisteredEvent.Create(Id, entry.Id, url, contentType.Id));

        return entry;
    }

    public void Unregister(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new DomainLayerException(Errors.EntryNotFound(entryId));

        _entries.Remove(entry);
        SetUpdated();

        AddDomainEvent(
            UrlUnregisteredEvent.Create(Id, entryId));
    }

    public IReadOnlyList<CatalogEntry> GetEntriesByType(ContentType contentType)
        => _entries.Where(e => e.ContentType == contentType).ToList().AsReadOnly();
}
