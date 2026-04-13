namespace Portfolio.Common.Seedwork.Errors;

public abstract class LayerException : Exception
{
    public string Layer { get; }
    public Error Error { get; }

    protected LayerException(string layer, Error error)
        : base($"[{layer}] {error}")
    {
        Layer = layer;
        Error = error;
    }

    protected LayerException(string layer, Error error, Exception innerException)
        : base($"[{layer}] {error}", innerException)
    {
        Layer = layer;
        Error = error;
    }
}
