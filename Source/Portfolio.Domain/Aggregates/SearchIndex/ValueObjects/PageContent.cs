using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

public sealed class PageContent : ValueObject
{
    public string Title { get; }
    public string? Description { get; }
    public string ContentHash { get; }
    public string? Snippet { get; }

    public PageContent(string title, string contentHash, string? description = null, string? snippet = null)
    {
        Title = Guard.AgainstNullOrWhiteSpace(title);
        ContentHash = Guard.AgainstNullOrWhiteSpace(contentHash);

        if (title.Length > 1024)
            throw new ArgumentException("Title cannot exceed 1024 characters.", nameof(title));

        if (snippet is not null && snippet.Length > 300)
            throw new ArgumentException("Snippet cannot exceed 300 characters.", nameof(snippet));

        Description = description;
        Snippet = snippet;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Title;
        yield return ContentHash;
        yield return Description;
        yield return Snippet;
    }
}
