using System.Text;
using Nexus.Core.Models;

namespace Nexus.Core.Utilities;

/// <summary>
/// Expands user-facing filename templates such as <c>{title} [{id}].{ext}</c>
/// against a <see cref="VideoInfo"/>. Every substituted value is sanitized so
/// the result is a safe single filename component.
/// </summary>
/// <remarks>
/// This is the app's own naming layer for display, history, and directory
/// composition. yt-dlp's own <c>-o</c> template is derived separately by the
/// argument builder; this engine keeps the two conceptually aligned but does not
/// depend on yt-dlp's syntax.
/// </remarks>
public static class OutputTemplateEngine
{
    /// <summary>Supported placeholder tokens (without braces).</summary>
    public static readonly IReadOnlyList<string> SupportedTokens =
        ["title", "channel", "uploader", "upload_date", "id", "resolution", "ext"];

    /// <summary>
    /// Renders <paramref name="template"/> for the given video. Unknown tokens are
    /// left untouched. The returned value is a sanitized filename component.
    /// </summary>
    /// <param name="template">Template string, e.g. "{title} [{id}].{ext}".</param>
    /// <param name="video">Metadata source.</param>
    /// <param name="extension">Resolved extension (without dot), used for {ext}.</param>
    /// <param name="resolution">Optional resolution label for {resolution}.</param>
    public static string Render(string template, VideoInfo video, string extension, string? resolution = null)
    {
        ArgumentNullException.ThrowIfNull(video);

        if (string.IsNullOrWhiteSpace(template))
        {
            template = "{title} [{id}].{ext}";
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["title"] = video.Title,
            ["channel"] = video.Uploader ?? video.ChannelId ?? "unknown",
            ["uploader"] = video.Uploader ?? "unknown",
            ["upload_date"] = video.UploadDate?.ToString("yyyy-MM-dd") ?? "unknown",
            ["id"] = video.Id,
            ["resolution"] = resolution ?? "",
            ["ext"] = extension.TrimStart('.')
        };

        var expanded = Expand(template, values);

        // The whole rendered string becomes one filename component.
        return FilenameSanitizer.SanitizeComponent(expanded);
    }

    private static string Expand(string template, IReadOnlyDictionary<string, string> values)
    {
        var sb = new StringBuilder(template.Length + 32);
        var i = 0;

        while (i < template.Length)
        {
            var open = template.IndexOf('{', i);
            if (open < 0)
            {
                sb.Append(template, i, template.Length - i);
                break;
            }

            sb.Append(template, i, open - i);

            var close = template.IndexOf('}', open + 1);
            if (close < 0)
            {
                // Unbalanced brace: emit the remainder verbatim.
                sb.Append(template, open, template.Length - open);
                break;
            }

            var token = template.Substring(open + 1, close - open - 1);
            if (values.TryGetValue(token, out var replacement))
            {
                sb.Append(replacement);
            }
            else
            {
                // Unknown token: keep it literally so misconfiguration is visible.
                sb.Append('{').Append(token).Append('}');
            }

            i = close + 1;
        }

        return sb.ToString();
    }
}
