using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace SiteBackend.Models.SearchEngine;

public class Word
{
    [Key]
    public int WordID  { get; set; }

    public string Text  { get; set; }
    
    // TODO: This should be a sparse vector
    // For pgVector embeddings
    [Column(TypeName = "vector(3)")]
    public SparseVector? Embedding { get; set; }
}