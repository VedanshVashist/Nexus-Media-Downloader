namespace Nexus.Core.Enums;

/// <summary>
/// User-facing quality presets shown on the Home page. Mapped to yt-dlp format
/// selectors by the download argument builder.
/// </summary>
public enum QualityPreference
{
    Best = 0,
    P2160 = 2160,
    P1440 = 1440,
    P1080 = 1080,
    P720 = 720,
    P480 = 480,
    P360 = 360,

    /// <summary>User picked a specific format id from the format list.</summary>
    Custom = -1
}
