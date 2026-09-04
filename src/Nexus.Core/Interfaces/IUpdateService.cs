namespace Nexus.Core.Interfaces;

/// <summary>Describes an available application update.</summary>
public sealed record UpdateInfo
{
    public required string LatestVersion { get; init; }
    public required string CurrentVersion { get; init; }
    public bool IsUpdateAvailable { get; init; }
    public string? ReleaseNotesUrl { get; init; }
    public string? DownloadUrl { get; init; }
}

/// <summary>
/// Application self-update abstraction. Kept UI-agnostic so an updater can be
/// added later without touching view-models. Current implementation may be a
/// skeleton that reports "up to date".
/// </summary>
public interface IUpdateService
{
    Task<UpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task<string> GetLatestVersionAsync(CancellationToken cancellationToken = default);
    Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    Task InstallUpdateAsync(string installerPath, CancellationToken cancellationToken = default);
}
