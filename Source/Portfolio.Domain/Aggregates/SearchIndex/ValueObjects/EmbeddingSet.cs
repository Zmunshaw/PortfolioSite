using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.SearchIndex.ValueObjects;

public sealed class EmbeddingSet : ValueObject
{
    public float[] DenseVector { get; }
    public int[] SparseIndices { get; }
    public float[] SparseValues { get; }
    public string TextHash { get; }

    public EmbeddingSet(float[] denseVector, int[] sparseIndices, float[] sparseValues, string textHash)
    {
        Guard.AgainstNull(denseVector);
        Guard.AgainstNull(sparseIndices);
        Guard.AgainstNull(sparseValues);
        TextHash = Guard.AgainstNullOrWhiteSpace(textHash);

        if (sparseIndices.Length != sparseValues.Length)
            throw new ArgumentException("SparseIndices and SparseValues must have the same length.");

        DenseVector = denseVector;
        SparseIndices = sparseIndices;
        SparseValues = sparseValues;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TextHash;
        yield return DenseVector.Length;
        yield return SparseIndices.Length;
    }
}
