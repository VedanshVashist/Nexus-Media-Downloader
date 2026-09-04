namespace Nexus.Core.Enums;

/// <summary>
/// The kind of artifact a download task produces. Drives which yt-dlp/FFmpeg
/// arguments are assembled and which formats are offered in the UI.
/// </summary>
public enum DownloadType
{
    /// <summary>Muxed or merged video + audio.</summary>
    Video = 0,

    /// <summary>Audio-only, optionally transcoded to a target codec/container.</summary>
    Audio = 1,

    /// <summary>Only the thumbnail image(s).</summary>
    Thumbnail = 2,

    /// <summary>Only subtitle track(s).</summary>
    Subtitle = 3,

    /// <summary>Only the metadata sidecar (info JSON).</summary>
    Metadata = 4
}
