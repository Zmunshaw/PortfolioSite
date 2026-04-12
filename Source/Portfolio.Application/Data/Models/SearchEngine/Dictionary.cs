using System.ComponentModel.DataAnnotations;
using Portfolio.Application.Data.Models.SearchEngine.Index;

namespace Portfolio.Application.Data.Models.SearchEngine;

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