using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace SiteBackend.Models.SearchEngine.Index;

public class Website
{
    [Key]
    public int WebsiteID { get; set; }
    
    public Sitemap? Sitemap { get; set; }
    public string Host { get; set; }

    public List<Page> Pages { get; set; } = new();
}

public class Page
{
    [Key]
    public int PageID { get; set; }
    
    public Content? Content { get; set; }
}

public class Content
{
    [Key]
    public int ContentID { get; set; }
    
    public string? ContentHash { get; set; }
    public string? Title { get; set; }
    public string? Text { get; set; }
    public string[]? Paragraphs { get; set; }
    public string[]? Images { get; set; }
    
    // For pgVector embeddings
    [Column(TypeName = "vector(768)")]
    public Vector? ContentEmbedding { get; set; }
}