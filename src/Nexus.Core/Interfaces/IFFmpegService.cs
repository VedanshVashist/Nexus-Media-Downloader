using Nexus.Core.Models;

namespace Nexus.Core.Interfaces;

/// <summary>
/// Abstraction over FFmpeg/ffprobe for post-processing operations. yt-dlp handles
/// most muxing internally; this service covers standalone operations and
/// availability detection. All invocations use process arguments, never a shell.
/// </summary>
public interface IFFmpegService
{
    /// <summary>True when a usable ffmpeg executable was located.</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>Detected ffmpeg version, or null when unavailable.</summary>
    Task<string?> GetVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>Merges a separate video and audio file into one container.</summary>
    Task MergeAsync(string videoPath, string audioPath, string outputPath, CancellationToken cancellationToken = default);

    /// <summary>Extracts/transcodes audio from a media file to the target codec/container.</summary>
    Task ExtractAudioAsync(string inputPath, string outputPath, string? audioCodec = null, CancellationToken cancellationToken = default);

    /// <summary>Converts a media file to a different container/format.</summary>
    Task ConvertAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default);

    /// <summary>Embeds metadata fields into a media file, writing to <paramref name="outputPath"/>.</summary>
    Task EmbedMetadataAsync(string inputPath, string outputPath, VideoInfo metadata, CancellationToken cancellationToken = default);

    /// <summary>Embeds a thumbnail image as cover art into a media file.</summary>
    Task EmbedThumbnailAsync(string mediaPath, string thumbnailPath, string outputPath, CancellationToken cancellationToken = default);

    /// <summary>Embeds chapter markers into a media file.</summary>
    Task EmbedChaptersAsync(string inputPath, string outputPath, IReadOnlyList<Chapter> chapters, CancellationToken cancellationToken = default);
}
