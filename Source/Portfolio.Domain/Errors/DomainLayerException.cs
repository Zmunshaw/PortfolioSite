using Portfolio.Common.Seedwork.Errors;

namespace Portfolio.Domain.Errors;

public sealed class DomainLayerException : LayerException
{
    private const string LayerName = "Domain";

    public DomainLayerException(Error error)
        : base(LayerName, error) { }

    public DomainLayerException(Error error, Exception innerException)
        : base(LayerName, error, innerException) { }
}
