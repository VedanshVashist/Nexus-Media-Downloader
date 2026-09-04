using Nexus.Core.Constants;
using Nexus.Core.Enums;

namespace Nexus.Core.Models;

/// <summary>
/// The complete, serializable application configuration. Persisted as JSON.
/// Grouped into sections mirroring the Settings UI. All properties have sensible
/// defaults so a fresh install is immediately usable.
/// </summary>
public sealed class AppSettings
{
    public GeneralSettings General { get; set; } = new();
    public DownloadSettings Downloads { get; set; } = new();
    public AppearanceSettings Appearance { get; set; } = new();
    public YtDlpSettings YtDlp { get; set; } = new();
    public FFmpegSettings FFmpeg { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public KeyboardSettings Keyboard { get; set; } = new();

    /// <summary>Set to true once the first-run wizard completes.</summary>
    public bool FirstRunCompleted { get; set; }

    /// <summary>Schema version, for forward-compatible migrations of the settings file.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Deep-ish clone for edit-then-save flows in the Settings view-model.</summary>
    public AppSettings Clone() => new()
    {
        General = General.Clone(),
        Downloads = Downloads.Clone(),
        Appearance = Appearance.Clone(),
        YtDlp = YtDlp.Clone(),
        FFmpeg = FFmpeg.Clone(),
        Notifications = Notifications.Clone(),
        Keyboard = Keyboard.Clone(),
        FirstRunCompleted = FirstRunCompleted,
        SchemaVersion = SchemaVersion
    };
}

public sealed class GeneralSettings
{
    public string Language { get; set; } = "en";
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; }
    public bool CheckForUpdates { get; set; } = true;

    public GeneralSettings Clone() => (GeneralSettings)MemberwiseClone();
}

public sealed class DownloadSettings
{
    public string? DefaultDownloadDirectory { get; set; }
    public string? VideoDirectory { get; set; }
    public string? AudioDirectory { get; set; }
    public string? ThumbnailDirectory { get; set; }
    public string? SubtitleDirectory { get; set; }

    public OutputContainer DefaultFormat { get; set; } = OutputContainer.Mp4;
    public QualityPreference DefaultQuality { get; set; } = QualityPreference.Best;

    public int MaxConcurrentDownloads { get; set; } = AppConstants.DefaultMaxConcurrentDownloads;
    public bool AutoStartDownloads { get; set; } = true;
    public bool RetryFailedDownloads { get; set; } = true;
    public int RetryCount { get; set; } = AppConstants.DefaultRetryCount;

    public bool CreateSubfolders { get; set; }
    public bool SubfolderByChannel { get; set; }
    public bool SubfolderByPlaylist { get; set; } = true;
    public bool SubfolderByDate { get; set; }

    public string OutputTemplate { get; set; } = AppConstants.DefaultOutputTemplate;

    /// <summary>Overwrite existing files instead of skipping/renaming. Off by default.</summary>
    public bool OverwriteExisting { get; set; }

    public DownloadSettings Clone()
    {
        var clone = (DownloadSettings)MemberwiseClone();
        return clone;
    }
}

public sealed class AppearanceSettings
{
    public ThemeType Theme { get; set; } = ThemeType.Midnight;

    /// <summary>Wallpaper file name stored in the app's Wallpapers folder (not a temp path).</summary>
    public string? WallpaperFileName { get; set; }

    public bool WallpaperEnabled { get; set; }
    public double WallpaperOpacity { get; set; } = 0.35;
    public double WallpaperBlur { get; set; } = 12;

    /// <summary>Overlay darkness 0–1 layered over the wallpaper for text legibility.</summary>
    public double WallpaperDarkness { get; set; } = 0.2;

    /// <summary>Stretch mode: Uniform, UniformToFill, Fill, None, Tile.</summary>
    public string WallpaperStretch { get; set; } = "UniformToFill";

    /// <summary>Optional accent color override as #RRGGBB or #AARRGGBB. Null uses the theme accent.</summary>
    public string? AccentColor { get; set; }

    public bool AnimationEnabled { get; set; } = true;
    public bool TransparencyEnabled { get; set; } = true;
    public bool CompactMode { get; set; }

    public AppearanceSettings Clone() => (AppearanceSettings)MemberwiseClone();
}

public sealed class YtDlpSettings
{
    /// <summary>Explicit path to yt-dlp. Null triggers auto-discovery (bundled tools folder, then PATH).</summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Extra raw arguments appended to every yt-dlp invocation. Parsed with a
    /// safe tokenizer and passed via ArgumentList — never through a shell.
    /// </summary>
    public string? CustomArguments { get; set; }

    public bool AutoUpdate { get; set; }

    public YtDlpSettings Clone() => (YtDlpSettings)MemberwiseClone();
}

public sealed class FFmpegSettings
{
    /// <summary>Explicit path to ffmpeg. Null triggers auto-discovery.</summary>
    public string? ExecutablePath { get; set; }

    /// <summary>Explicit path to ffprobe. Null triggers auto-discovery.</summary>
    public string? FFprobePath { get; set; }

    public bool AutoUpdate { get; set; }

    public FFmpegSettings Clone() => (FFmpegSettings)MemberwiseClone();
}

public sealed class NotificationSettings
{
    public bool DownloadStarted { get; set; } = true;
    public bool DownloadCompleted { get; set; } = true;
    public bool DownloadFailed { get; set; } = true;

    public NotificationSettings Clone() => (NotificationSettings)MemberwiseClone();
}

public sealed class KeyboardSettings
{
    /// <summary>Command name → gesture string (e.g. "Analyze" → "Enter"). Configurable where practical.</summary>
    public Dictionary<string, string> Shortcuts { get; set; } = new()
    {
        ["PasteUrl"] = "Ctrl+V",
        ["Analyze"] = "Enter",
        ["Download"] = "Ctrl+Enter",
        ["PasteAndAnalyze"] = "Ctrl+Shift+V",
        ["RemoveQueueItem"] = "Delete",
        ["OpenSettings"] = "Ctrl+OemComma"
    };

    public KeyboardSettings Clone() => new()
    {
        Shortcuts = new Dictionary<string, string>(Shortcuts)
    };
}
