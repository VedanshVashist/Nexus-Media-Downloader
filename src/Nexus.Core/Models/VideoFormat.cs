namespace Nexus.Core.Models;

/// <summary>
/// A single downloadable stream (muxed, video-only, or audio-only) reported by
/// yt-dlp. Immutable; produced by the yt-dlp JSON mapper.
/// </summary>
public sealed record VideoFormat
{
    /// <summary>yt-dlp format identifier, e.g. "137", "251", "22".</summary>
    public required string FormatId { get; init; }

    /// <summary>File extension yt-dlp assigns, e.g. "mp4", "webm", "m4a".</summary>
    public string? Extension { get; init; }

    /// <summary>Container/format note, e.g. "mp4_dash".</summary>
    public string? Container { get; init; }

    /// <summary>Human-readable resolution, e.g. "1920x1080" or "audio only".</summary>
    public string? Resolution { get; init; }

    public int? Width { get; init; }
    public int? Height { get; init; }
    public double? Fps { get; init; }

    /// <summary>Video codec, e.g. "avc1.640028". "none" indicates audio-only.</summary>
    public string? VideoCodec { get; init; }

    /// <summary>Audio codec, e.g. "mp4a.40.2". "none" indicates video-only.</summary>
    public string? AudioCodec { get; init; }

    /// <summary>Audio bitrate in kbps, when known.</summary>
    public double? AudioBitrate { get; init; }

    /// <summary>Video bitrate in kbps, when known.</summary>
    public double? VideoBitrate { get; init; }

    /// <summary>Exact file size in bytes, when yt-dlp reports it.</summary>
    public long? FileSize { get; init; }

    /// <summary>Approximate file size in bytes, when the exact size is unknown.</summary>
    public long? FileSizeApproximation { get; init; }

    /// <summary>Dynamic range, e.g. "SDR", "HDR10".</summary>
    public string? DynamicRange { get; init; }

    /// <summary>Delivery protocol, e.g. "https", "m3u8_native".</summary>
    public string? Protocol { get; init; }

    /// <summary>yt-dlp numeric quality hint, used as a tiebreaker in sorting.</summary>
    public double? Quality { get; init; }

    /// <summary>A short human-readable label yt-dlp provides for the format.</summary>
    public string? FormatNote { get; init; }

    /// <summary>True when the stream carries video but no audio.</summary>
    public bool IsVideoOnly =>
        !string.IsNullOrEmpty(VideoCodec) && VideoCodec != "none" &&
        (string.IsNullOrEmpty(AudioCodec) || AudioCodec == "none");

    /// <summary>True when the stream carries audio but no video.</summary>
    public bool IsAudioOnly =>
        !string.IsNullOrEmpty(AudioCodec) && AudioCodec != "none" &&
        (string.IsNullOrEmpty(VideoCodec) || VideoCodec == "none");

    /// <summary>Best-effort size in bytes: exact if present, otherwise the approximation.</summary>
    public long? EffectiveFileSize => FileSize ?? FileSizeApproximation;
}
