using Nexus.Core.DTOs;

namespace Nexus.Core.Interfaces;

/// <summary>
/// Locates and validates external tools (yt-dlp, ffmpeg, ffprobe), reports their
/// versions, and can optionally update supported tools from trusted official
/// sources. Discovery order: explicit settings path, bundled tools folder, PATH.
/// </summary>
public interface IDependencyManager
{
    /// <summary>Probes all known dependencies and returns their statuses.</summary>
    Task<IReadOnlyList<DependencyStatus>> CheckAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Probes a single dependency by logical name ("yt-dlp", "ffmpeg", "ffprobe").</summary>
    Task<DependencyStatus> CheckAsync(string dependencyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the absolute path to a dependency's executable, or null when not
    /// found. Never downloads implicitly.
    /// </summary>
    Task<string?> ResolvePathAsync(string dependencyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads/updates yt-dlp from its official release location into the app's
    /// tools folder. Only trusted sources are used. Returns the resulting path.
    /// </summary>
    Task<string> UpdateYtDlpAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
