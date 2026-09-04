namespace Nexus.Core.Constants;

/// <summary>
/// Application-wide constant values. Anything a user or deployment might want to
/// change belongs in settings, not here; this holds true invariants and defaults.
/// </summary>
public static class AppConstants
{
    /// <summary>Product name used for folders, window titles, and logs.</summary>
    public const string AppName = "Nexus";

    /// <summary>Folder name created under %LocalAppData% for app data.</summary>
    public const string DataFolderName = "Nexus";

    /// <summary>SQLite database file name.</summary>
    public const string DatabaseFileName = "nexus.db";

    /// <summary>Settings file name (JSON) stored in the data folder.</summary>
    public const string SettingsFileName = "settings.json";

    /// <summary>Subfolder for imported wallpapers.</summary>
    public const string WallpapersFolderName = "Wallpapers";

    /// <summary>Subfolder for cached thumbnails.</summary>
    public const string ThumbnailCacheFolderName = "ThumbnailCache";

    /// <summary>Subfolder for rolling log files.</summary>
    public const string LogsFolderName = "logs";

    /// <summary>Subfolder (next to the executable) where bundled tools are expected.</summary>
    public const string BundledToolsFolderName = "tools";

    /// <summary>Default number of simultaneous downloads.</summary>
    public const int DefaultMaxConcurrentDownloads = 3;

    /// <summary>Hard ceiling to prevent uncontrolled process spawning.</summary>
    public const int MaxAllowedConcurrentDownloads = 10;

    /// <summary>Default retry count for failed downloads.</summary>
    public const int DefaultRetryCount = 2;

    /// <summary>Default output filename template.</summary>
    public const string DefaultOutputTemplate = "{title} [{id}].{ext}";
}
