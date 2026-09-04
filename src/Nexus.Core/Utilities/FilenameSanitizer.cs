using System.Text;

namespace Nexus.Core.Utilities;

/// <summary>
/// Produces safe Windows filenames and prevents path traversal. Used wherever
/// user-influenced text (titles, channel names, templates) becomes a file path.
/// </summary>
public static class FilenameSanitizer
{
    // Characters illegal in Windows filenames, plus control chars handled separately.
    private static readonly char[] InvalidChars =
        ['<', '>', ':', '\"', '/', '\\', '|', '?', '*'];

    // Device names reserved by Windows regardless of extension.
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private const int MaxComponentLength = 200;

    /// <summary>
    /// Sanitizes a single path component (not a full path). Invalid and control
    /// characters are replaced, reserved names are escaped, length is bounded, and
    /// the result never resolves to a traversal segment.
    /// </summary>
    /// <param name="name">The raw candidate component.</param>
    /// <param name="replacement">Character substituted for illegal characters.</param>
    public static string SanitizeComponent(string? name, char replacement = '_')
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "untitled";
        }

        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (ch < 32 || Array.IndexOf(InvalidChars, ch) >= 0)
            {
                sb.Append(replacement);
            }
            else
            {
                sb.Append(ch);
            }
        }

        var result = sb.ToString().Trim();

        // Collapse pure-dot / traversal components.
        if (result is "." or ".." || result.Trim('.').Length == 0)
        {
            return "untitled";
        }

        // Windows strips trailing dots and spaces from names; do it deterministically.
        result = result.TrimEnd('.', ' ');
        if (result.Length == 0)
        {
            result = "untitled";
        }

        // Escape reserved device names (check the part before the first dot).
        var stem = result.Split('.', 2)[0];
        if (ReservedNames.Contains(stem))
        {
            result = replacement + result;
        }

        if (result.Length > MaxComponentLength)
        {
            result = result[..MaxComponentLength].TrimEnd('.', ' ');
        }

        return result.Length == 0 ? "untitled" : result;
    }

    /// <summary>
    /// Validates that <paramref name="candidatePath"/> stays within
    /// <paramref name="baseDirectory"/> after full resolution. Guards against
    /// traversal via <c>..</c> segments or absolute path injection.
    /// </summary>
    public static bool IsWithinDirectory(string baseDirectory, string candidatePath)
    {
        var baseFull = Path.GetFullPath(AppendSeparator(baseDirectory));
        var candidateFull = Path.GetFullPath(candidatePath);

        return candidateFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase);
    }

    private static string AppendSeparator(string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar) &&
            !path.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return path + Path.DirectorySeparatorChar;
        }

        return path;
    }
}
