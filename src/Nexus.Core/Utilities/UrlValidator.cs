using System.Diagnostics.CodeAnalysis;

namespace Nexus.Core.Utilities;

/// <summary>
/// Validates and normalizes user-supplied URLs before they reach yt-dlp.
/// Treats all input as untrusted: only absolute http/https URLs are accepted.
/// </summary>
public static class UrlValidator
{
    /// <summary>
    /// Returns true when <paramref name="candidate"/> is a well-formed absolute
    /// http/https URL. On success, <paramref name="normalized"/> holds the trimmed,
    /// canonical form.
    /// </summary>
    public static bool IsValid(string? candidate, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var trimmed = candidate.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        // Must have a real host.
        if (string.IsNullOrEmpty(uri.Host) || !uri.Host.Contains('.'))
        {
            return false;
        }

        normalized = uri.AbsoluteUri;
        return true;
    }

    /// <summary>Convenience overload without the out parameter.</summary>
    public static bool IsValid(string? candidate) => IsValid(candidate, out _);

    /// <summary>
    /// Extracts all valid, distinct URLs from a block of text (e.g. a pasted list
    /// or a dropped text file). Order is preserved; duplicates removed.
    /// </summary>
    public static IReadOnlyList<string> ExtractUrls(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<string>();

        var tokens = text.Split(
            [' ', '\t', '\r', '\n', ',', ';', '\"', '\'', '<', '>'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var token in tokens)
        {
            if (IsValid(token, out var normalized) && seen.Add(normalized))
            {
                results.Add(normalized);
            }
        }

        return results;
    }
}
