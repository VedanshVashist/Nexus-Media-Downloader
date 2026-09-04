using System.Reflection;
using Microsoft.Extensions.Logging;
using Nexus.Core.Constants;
using Nexus.Core.Interfaces;

namespace Nexus.Infrastructure.Services;

/// <summary>
/// Minimal <see cref="IUpdateService"/> implementation. It reports the current
/// assembly version and, by default, "up to date". The architecture allows a real
/// GitHub-releases-backed updater to be dropped in later without touching the UI.
/// </summary>
public sealed class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
    }

    public Task<UpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var current = CurrentVersion();
        _logger.LogInformation("Update check requested (current version {Version}).", current);

        // No update source wired up yet; report current as latest.
        return Task.FromResult(new UpdateInfo
        {
            CurrentVersion = current,
            LatestVersion = current,
            IsUpdateAvailable = false,
            ReleaseNotesUrl = AppLinks.GitHub
        });
    }

    public Task<string> GetLatestVersionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CurrentVersion());

    public Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Automatic update download is not configured in this build.");

    public Task InstallUpdateAsync(string installerPath, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Automatic update installation is not configured in this build.");

    private static string CurrentVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version
            ?? Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString(3) ?? "0.1.0";
    }
}
