using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using Pgvector;
using SiteBackend.Database;

namespace SiteBackend.Models.SearchEngine.Index;

public class Website
{
    public Website() {}

    public Website(string host, Sitemap? sitemap = null, List<Page>? pages = null)
    {
        Host = host;
        Pages = pages ?? [new Page(host, this)];
        Sitemap = sitemap ?? new Sitemap(this, host, Pages.Select(pg => pg.Url).ToList());
    }
    
    [Key]
    public int WebsiteID { get; set; }
    
    public Sitemap Sitemap { get; set; }
    public string Host { get; set; }

    public List<Page> Pages { get; set; } = new();
}

public class Page
{
    public Page() {}

    public Page(string url, Website website, Content? content = null)
    {
        Website = website;
        Content = content ?? new(this, "", "");
        Url = new(url, website.Sitemap, this);
    }
    
    [Key]
    public int PageID { get; set; }
    public Url Url { get; set; }
    public Content Content { get; set; } = new();
    
    public DateTime? LastCrawlAttempt {get; set;}
    public DateTime? LastCrawled { get; set; }
    // FKs
    public Website Website { get; set; }
}

public class Content
{
    public Content() {}

    public Content(Page page, string? title = null, string? text = null)
    {
        Page = page;
        Title = title;
        Text = text;
    }
    
    [Key]
    public int ContentID { get; set; }
    public Page Page { get; set; }
    
    public string? Title { get; set; }
    public string? Text { get; set; }
    public string[]? Paragraphs { get; set; }
    public string[]? Images { get; set; }
    public string? ContentHash { get; set; }
    
    // For pgVector embeddings
    [Column(TypeName = "vector(768)")]
    public Vector? ContentEmbedding { get; set; }
}