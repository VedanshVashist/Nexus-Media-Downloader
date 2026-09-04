using Nexus.Core.Enums;
using Nexus.Core.Models;

namespace Nexus.Core.Utilities;

/// <summary>
/// Pure helpers for filtering and ranking <see cref="VideoFormat"/> lists and for
/// translating UI quality/container choices into yt-dlp format selector strings.
/// Kept free of side effects so it is trivially unit-testable.
/// </summary>
public static class FormatFilter
{
    /// <summary>Formats that carry a video stream (muxed or video-only), ranked best-first.</summary>
    public static IReadOnlyList<VideoFormat> VideoFormats(IEnumerable<VideoFormat> formats) =>
        formats
            .Where(f => !f.IsAudioOnly && f.Height is > 0)
            .OrderByDescending(f => f.Height ?? 0)
            .ThenByDescending(f => f.Fps ?? 0)
            .ThenByDescending(f => f.VideoBitrate ?? 0)
            .ToList();

    /// <summary>Audio-only formats, ranked by bitrate best-first.</summary>
    public static IReadOnlyList<VideoFormat> AudioFormats(IEnumerable<VideoFormat> formats) =>
        formats
            .Where(f => f.IsAudioOnly)
            .OrderByDescending(f => f.AudioBitrate ?? 0)
            .ThenByDescending(f => f.Quality ?? 0)
            .ToList();

    /// <summary>Distinct available heights (e.g. 2160, 1080, 720), descending.</summary>
    public static IReadOnlyList<int> AvailableHeights(IEnumerable<VideoFormat> formats) =>
        formats
            .Where(f => f.Height is > 0)
            .Select(f => f.Height!.Value)
            .Distinct()
            .OrderByDescending(h => h)
            .ToList();

    /// <summary>
    /// Builds a yt-dlp <c>-f</c> selector for a video download honoring the quality
    /// preference. Prefers merging best video ≤ target height with best audio, and
    /// falls back to the best single stream.
    /// </summary>
    public static string BuildVideoSelector(QualityPreference quality, string? customFormatId = null)
    {
        if (quality == QualityPreference.Custom && !string.IsNullOrWhiteSpace(customFormatId))
        {
            // A specific stream; still add best audio when the chosen one is video-only.
            return $"{customFormatId}+bestaudio/{customFormatId}";
        }

        if (quality == QualityPreference.Best)
        {
            return "bestvideo*+bestaudio/best";
        }

        var height = (int)quality;
        return $"bestvideo[height<={height}]+bestaudio/best[height<={height}]/best";
    }

    /// <summary>Builds a yt-dlp <c>-f</c> selector for an audio-only download.</summary>
    public static string BuildAudioSelector(string? customFormatId = null)
    {
        if (!string.IsNullOrWhiteSpace(customFormatId))
        {
            return customFormatId;
        }

        return "bestaudio/best";
    }

    /// <summary>
    /// Maps an <see cref="OutputContainer"/> to a target extension for merge/convert,
    /// or null when the container is Auto (keep source).
    /// </summary>
    public static string? ContainerExtension(OutputContainer container, string? custom = null) => container switch
    {
        OutputContainer.Auto => null,
        OutputContainer.Mp4 => "mp4",
        OutputContainer.Mkv => "mkv",
        OutputContainer.Webm => "webm",
        OutputContainer.Mp3 => "mp3",
        OutputContainer.M4a => "m4a",
        OutputContainer.Opus => "opus",
        OutputContainer.Flac => "flac",
        OutputContainer.Wav => "wav",
        OutputContainer.Custom => string.IsNullOrWhiteSpace(custom) ? null : custom.TrimStart('.'),
        _ => null
    };

    /// <summary>True when the container is an audio-only target.</summary>
    public static bool IsAudioContainer(OutputContainer container) => container switch
    {
        OutputContainer.Mp3 or OutputContainer.M4a or OutputContainer.Opus
            or OutputContainer.Flac or OutputContainer.Wav => true,
        _ => false
    };
}
