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

    public Sitemap Sitemap { get; set; }
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
        Website = website;
        Content = content ?? new(this, "", "");
        Url = new(url, website.Sitemap, this);
    }

    [Key] public int PageID { get; set; }

    public Url Url { get; set; }
    public Content Content { get; set; }

    public DateTime? LastCrawlAttempt { get; set; }

    public DateTime? LastCrawled { get; set; }

    // FKs
    public Website Website { get; set; }
}

public class Content
{
    public Content()
    {
    }

    public Content(Page page, string? title = null, string? text = null)
    {
        Page = page;
        Title = title;
        Text = text;
    }

    [Key] public int ContentID { get; set; }

    public Page Page { get; set; }

    [MaxLength(1024)] public string? Title { get; set; }

    // (2 * MaxLength)Bytes I think (26ish mb assuming 25 * 1024 * 1024)
    // assumption being that C# chars and pg chars are equal
    [MaxLength(25 * 1024 * 1024)] public string? Text { get; set; }

    public string? ContentHash { get; set; }

    public List<Word>? Words { get; set; }
    public List<TextEmbedding> Embeddings { get; set; } = new();
    public bool NeedsEmbedding { get; set; } = false;
}

/// <summary>
/// Embedding representation of a paragraph
/// </summary>
public class TextEmbedding
{
    public TextEmbedding()
    {
    }

    public TextEmbedding(string text, Vector denseEmbedding, SparseVector sparseEmbedding)
    {
        RawText = text;
        DenseEmbedding = denseEmbedding;
        SparseEmbedding = sparseEmbedding;
    }

    [Key] public int TextEmbeddingID { get; set; }

    public Content? Content { get; set; }

    // Meta
    public string? EmbeddingHash { get; set; }
    public string? RawText { get; set; }

    // TODO: Move to a settings file or smth
    // Embeddings
    [Column(TypeName = "sparsevec")] public SparseVector? SparseEmbedding { get; set; }

    // TODO: Move to a settings file or smth
    [Column(TypeName = "vector(768)")] public Vector? DenseEmbedding { get; set; }
}