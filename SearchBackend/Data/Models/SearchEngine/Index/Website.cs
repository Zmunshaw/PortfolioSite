using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace SiteBackend.Models.SearchEngine.Index;

public class Website
{
    public Website()
    {
    }

    public Website(string host, Sitemap? sitemap = null, List<Page>? pages = null)
    {
        Host = host;
        Pages = pages ?? [new Page(host, this)];
        Sitemap = sitemap ?? new Sitemap(this, host, Pages.Select(pg => pg.Url).ToList());
    }

    [Key] public int WebsiteID { get; set; }

    public int? SitemapID { get; set; }
    public Sitemap? Sitemap { get; set; }

    public string Host { get; set; }

    public List<Page> Pages { get; set; } = new();
}

public class Page
{
    public Page()
    {
    }

    public Page(string url, Website website, Content? content = null)
    {
        WebsiteID = website.WebsiteID;
        Website = website;
        Content = content ?? new(this, "", "");
        Url = new(url, website.Sitemap, this);
    }

    [Key] public int PageID { get; set; }

    // One-to-one with Url
    public int? UrlID { get; set; }
    public Url? Url { get; set; }

    // One-to-one with Content
    public int? ContentID { get; set; }
    public Content? Content { get; set; }

    public DateTime? LastCrawlAttempt { get; set; }
    public int CrawlAttempts { get; set; } = 0;
    public DateTime? LastCrawled { get; set; }

    // One-to-many relationships (outlinks and inlinks point to other Urls, not Pages)
    public List<Url> Outlinks { get; set; } = new();
    public List<Url> InLinks { get; set; } = new();

    // Foreign key to Website
    public int WebsiteID { get; set; }
    public Website Website { get; set; }
}

public class Content
{
    public Content()
    {
    }

    public Content(Page page, string? title = null, string? text = null)
    {
        PageID = page.PageID;
        Page = page;
        Title = title;
        Text = text;
    }

    [Key] public int ContentID { get; set; }

    // Foreign key to Page (one-to-one)
    public int PageID { get; set; }
    public Page Page { get; set; }

    public string? ContentHash { get; set; }
    [MaxLength(1024)] public string? Title { get; set; }

    public string? Description { get; set; }

    // (2 * MaxLength)Bytes I think (26ish mb assuming 25 * 1024 * 1024)
    // assumption being that C# chars and pg chars are equal
    [MaxLength(25 * 1024 * 1024)] public string? Text { get; set; }

    public List<Word>? Words { get; set; }
    public List<TextEmbedding> Embeddings { get; set; } = new();

    public DateTime? LastScraped { get; set; }

    // Enhanced metadata fields
    [MaxLength(512)] public string? Author { get; set; }
    public DateTime? PublishedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    [MaxLength(2048)] public string? CanonicalUrl { get; set; }
    [MaxLength(10)] public string? Language { get; set; }
    public int? WordCount { get; set; }

    // Structured data stored as JSON
    [MaxLength(4096)] public string? HeadersJson { get; set; }  // JSON of h1, h2, h3
    [MaxLength(4096)] public string? OpenGraphJson { get; set; }  // JSON of OG tags
    [MaxLength(2048)] public string? TwitterCardJson { get; set; }  // JSON of Twitter Card

    // Link analysis
    public int? InternalLinkCount { get; set; }
    public int? ExternalLinkCount { get; set; }
}

/// <summary>
/// Embedding representation of a paragraph
/// </summary>
public class TextEmbedding
{
    public TextEmbedding()
    {
    }

    public TextEmbedding(Vector denseEmbedding, SparseVector sparseEmbedding)
    {
        DenseEmbedding = denseEmbedding;
        SparseEmbedding = sparseEmbedding;
    }

    [Key] public int TextEmbeddingID { get; set; }

    // Foreign key to Content
    public int? ContentID { get; set; }
    public Content? Content { get; set; }

    public string? TextHash { get; set; }

    // Embeddings
    [Column(TypeName = "sparsevec")] public SparseVector? SparseEmbedding { get; set; }
    [Column(TypeName = "vector(768)")] public Vector? DenseEmbedding { get; set; }
}