using Nexus.Core.Constants;

namespace Nexus.Infrastructure.Settings;

/// <summary>
/// Resolves well-known application data locations under the user's LocalAppData,
/// creating directories on demand. Centralizes path policy so no other component
/// hardcodes locations.
/// </summary>
public sealed class AppPaths
{
    /// <summary>Root data directory, e.g. %LocalAppData%\Nexus.</summary>
    public string DataDirectory { get; }

    public string DatabasePath { get; }
    public string SettingsPath { get; }
    public string WallpapersDirectory { get; }
    public string ThumbnailCacheDirectory { get; }
    public string LogsDirectory { get; }

    /// <summary>Folder next to the executable where bundled tools are expected.</summary>
    public string BundledToolsDirectory { get; }

    public AppPaths()
    {
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);

        DataDirectory = Path.Combine(localAppData, AppConstants.DataFolderName);
        DatabasePath = Path.Combine(DataDirectory, AppConstants.DatabaseFileName);
        SettingsPath = Path.Combine(DataDirectory, AppConstants.SettingsFileName);
        WallpapersDirectory = Path.Combine(DataDirectory, AppConstants.WallpapersFolderName);
        ThumbnailCacheDirectory = Path.Combine(DataDirectory, AppConstants.ThumbnailCacheFolderName);
        LogsDirectory = Path.Combine(DataDirectory, AppConstants.LogsFolderName);

        var exeDir = AppContext.BaseDirectory;
        BundledToolsDirectory = Path.Combine(exeDir, AppConstants.BundledToolsFolderName);
    }

    /// <summary>Creates all app data directories if they do not already exist.</summary>
    public void EnsureCreated()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(WallpapersDirectory);
        Directory.CreateDirectory(ThumbnailCacheDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }

    /// <summary>The default download directory: the user's Videos\Nexus folder.</summary>
    public static string DefaultDownloadDirectory()
    {
        var videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        if (string.IsNullOrEmpty(videos))
        {
            videos = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(videos, AppConstants.AppName);
    }
}
