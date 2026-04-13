using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.Sitemap.ValueObjects;

public sealed class LanguageCode : ValueObject
{
    public static LanguageCode Default => new("x-default");

    public string Value { get; }

    public LanguageCode(string code)
    {
        Guard.AgainstNullOrWhiteSpace(code);

        // ISO 639 (2-3 chars), region variants (en-us, zh-cn), or x-default
        Value = code.ToLowerInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
