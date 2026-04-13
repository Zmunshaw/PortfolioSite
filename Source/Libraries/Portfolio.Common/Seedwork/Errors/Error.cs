namespace Portfolio.Common.Seedwork.Errors;

public sealed record Error(string Code, string Message)
{
    public override string ToString() => $"[{Code}] {Message}";
}
