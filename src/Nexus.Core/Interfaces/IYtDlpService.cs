using Nexus.Core.DTOs;
using Nexus.Core.Models;

namespace Nexus.Core.Interfaces;

/// <summary>
/// Abstraction over the yt-dlp executable. Implementations must invoke the tool
/// via process arguments (never a shell) and convert structured JSON output into
/// the application's own models.
/// </summary>
public interface IYtDlpService
{
    /// <summary>Returns the detected yt-dlp version, or null when unavailable.</summary>
    Task<string?> GetVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a URL, returning either a single <see cref="VideoInfo"/> or a
    /// <see cref="PlaylistInfo"/>. Uses flat extraction for playlists to stay fast.
    /// </summary>
    Task<UrlAnalysisResult> AnalyzeAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Extracts full metadata for a single video URL.</summary>
    Task<VideoInfo> GetVideoInfoAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a download for the given task, reporting progress and honoring
    /// cancellation. Returns the final output file path on success.
    /// </summary>
    /// <param name="task">The task describing what and how to download.</param>
    /// <param name="settings">Current application settings (paths, templates, tool config).</param>
    /// <param name="progress">Receives progress snapshots parsed from yt-dlp output.</param>
    /// <param name="cancellationToken">Cancels the underlying process.</param>
    Task<string> DownloadAsync(
        DownloadTask task,
        AppSettings settings,
        IProgress<DownloadProgress> progress,
        CancellationToken cancellationToken = default);
}
