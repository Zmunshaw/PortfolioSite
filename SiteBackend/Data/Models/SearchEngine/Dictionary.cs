using System.ComponentModel.DataAnnotations;
using Pgvector;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Models.SearchEngine;

public class Word
{
    public Word()
    {
    }

    public Word(string word)
    {
        Text = word;
    }

    [Key] public int WordID { get; set; }

    public List<Content> Contents { get; set; }

    public string Text { get; set; }

    [DataType("sparsevec")] public SparseVector? SparseVector { get; set; }
}