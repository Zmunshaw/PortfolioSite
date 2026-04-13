using System.Text.RegularExpressions;
using System.Web;
using Portfolio.Common.Seedwork.Aggregates;
using Portfolio.Common.Seedwork.Guards;

namespace Portfolio.Domain.Aggregates.Shared;

public sealed partial class Url : ValueObject
{
    private static readonly HashSet<string> TrackingParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content",
        "fbclid", "gclid", "gclsrc", "dclid", "msclkid",
        "mc_cid", "mc_eid",
        "yclid", "twclid", "_hsenc", "_hsmi", "hsCtaTracking",
        "igshid", "si", "ref", "ref_src", "ref_url"
    };

    public string Value { get; }
    public string Original { get; }
    public string Scheme { get; }
    public string Host { get; }
    public int Port { get; }
    public string Path { get; }
    public string? Query { get; }
    public bool WasHealed { get; }

    public Url(string uri)
    {
        Guard.AgainstNullOrWhiteSpace(uri);

        Original = uri;
        var healed = Heal(uri);
        WasHealed = healed != uri;

        if (!Uri.TryCreate(healed, UriKind.Absolute, out var parsed))
            throw new ArgumentException($"'{uri}' is not a valid absolute URI.", nameof(uri));

        if (parsed.Scheme is not ("http" or "https"))
            throw new ArgumentException($"URL scheme must be http or https, got '{parsed.Scheme}'.", nameof(uri));

        parsed = Normalize(parsed);

        Value = parsed.AbsoluteUri;
        Scheme = parsed.Scheme;
        Host = parsed.Host;
        Port = parsed.Port;
        Path = parsed.AbsolutePath;
        Query = string.IsNullOrEmpty(parsed.Query) ? null : parsed.Query;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    // --- Healing: best-effort correction of malformed input ---

    private static string Heal(string raw)
    {
        // Strip whitespace and control characters
        raw = ControlCharRegex().Replace(raw.Trim(), "");

        // Fix common scheme typos
        raw = SchemeTypoRegex().Replace(raw, "https://");

        // Missing scheme entirely — if it looks like a domain, prepend https
        if (!raw.Contains("://"))
            raw = "https://" + raw;

        return raw;
    }

    // --- Normalization: deterministic canonicalization ---

    private static Uri Normalize(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant()
        };

        // Remove default ports
        if ((builder.Scheme == "http" && builder.Port == 80) ||
            (builder.Scheme == "https" && builder.Port == 443))
        {
            builder.Port = -1;
        }

        // Remove fragment — not meaningful server-side
        builder.Fragment = "";

        // Normalize path: decode unnecessary percent-encoding, remove trailing slash
        var path = Uri.UnescapeDataString(builder.Path);
        if (path.Length > 1 && path.EndsWith('/'))
            path = path.TrimEnd('/');
        if (string.IsNullOrEmpty(path))
            path = "/";
        builder.Path = path;

        // Sort query params and strip tracking params
        builder.Query = NormalizeQuery(uri.Query);

        return builder.Uri;
    }

    private static string NormalizeQuery(string? query)
    {
        if (string.IsNullOrEmpty(query))
            return "";

        var parsed = HttpUtility.ParseQueryString(query);
        var cleaned = new SortedDictionary<string, string?>(StringComparer.Ordinal);

        foreach (string? key in parsed)
        {
            if (key is null) continue;
            if (TrackingParams.Contains(key)) continue;
            cleaned[key] = parsed[key];
        }

        if (cleaned.Count == 0)
            return "";

        return string.Join("&", cleaned.Select(kv =>
            string.IsNullOrEmpty(kv.Value)
                ? Uri.EscapeDataString(kv.Key)
                : $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
    }

    [GeneratedRegex(@"[\x00-\x1F\x7F]")]
    private static partial Regex ControlCharRegex();

    [GeneratedRegex(@"^(htps?|htts?|hps?|hhttps?|httpss?)://", RegexOptions.IgnoreCase)]
    private static partial Regex SchemeTypoRegex();
}
