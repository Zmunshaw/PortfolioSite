using System.ComponentModel.DataAnnotations;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Models.SearchEngine;

public class Word
{
    [Key] public int WordID { get; set; }

    public List<Content> Contents { get; set; }
    public string Text { get; set; }
}