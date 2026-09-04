using Nexus.Core.Enums;

namespace Nexus.Core.Models;

/// <summary>Describes a subtitle/caption track available for a video.</summary>
public sealed record SubtitleInfo
{
    /// <summary>Human-readable language name, e.g. "English".</summary>
    public required string Language { get; init; }

    /// <summary>Language code as reported by yt-dlp, e.g. "en", "en-US".</summary>
    public required string LanguageCode { get; init; }

    /// <summary>Whether these are auto-generated captions.</summary>
    public bool IsAutomatic { get; init; }

    /// <summary>Convenience view over <see cref="IsAutomatic"/>.</summary>
    public SubtitleType Type => IsAutomatic ? SubtitleType.Automatic : SubtitleType.Manual;

    /// <summary>Available subtitle formats for this track, e.g. "srt", "vtt", "ass".</summary>
    public IReadOnlyList<string> Formats { get; init; } = [];
}
