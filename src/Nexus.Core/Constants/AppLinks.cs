namespace Nexus.Core.Constants;

/// <summary>
/// External links surfaced on the About page. Placeholders until a real
/// repository exists; centralized so they are never hardcoded across the UI.
/// </summary>
public static class AppLinks
{
    public const string GitHub = "https://github.com/your-org/nexus";
    public const string Documentation = "https://github.com/your-org/nexus#readme";
    public const string ReportIssue = "https://github.com/your-org/nexus/issues";

    /// <summary>Official yt-dlp releases — the only trusted source for auto-updates.</summary>
    public const string YtDlpReleasesApi = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";

    /// <summary>Official yt-dlp Windows binary download.</summary>
    public const string YtDlpWindowsBinary = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
}
