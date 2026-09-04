using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nexus.App.Services;
using Nexus.Core.Constants;
using Nexus.Core.DTOs;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.App.ViewModels;

/// <summary>
/// The Settings page. Edits an in-memory draft of <see cref="AppSettings"/>, applies
/// theme/accent changes live for immediate feedback, and persists everything on save.
/// Also surfaces dependency status/updates and wallpaper management.
/// </summary>
public sealed partial class SettingsViewModel : PageViewModel
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _themeService;
    private readonly IDownloadManager _manager;
    private readonly IDependencyManager _dependencies;
    private readonly IWallpaperService _wallpapers;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notifications;
    private readonly ILogger<SettingsViewModel> _logger;

    private AppSettings _draft;
    private bool _loading;

    public SettingsViewModel(
        ISettingsService settings,
        IThemeService themeService,
        IDownloadManager manager,
        IDependencyManager dependencies,
        IWallpaperService wallpapers,
        IDialogService dialogs,
        INotificationService notifications,
        ILogger<SettingsViewModel> logger)
        : base("settings", "Settings", NavGlyph.Settings)
    {
        _settings = settings;
        _themeService = themeService;
        _manager = manager;
        _dependencies = dependencies;
        _wallpapers = wallpapers;
        _dialogs = dialogs;
        _notifications = notifications;
        _logger = logger;

        IsPrimaryNavigation = false;
        _draft = settings.Current.Clone();
        LoadFromDraft();
        RefreshWallpapers();
    }

    // --- Choice lists ---

    public IReadOnlyList<LabeledValue<ThemeType>> ThemeChoices { get; } =
    [
        new(ThemeType.Midnight, "Midnight"),
        new(ThemeType.Aurora, "Aurora"),
        new(ThemeType.Crimson, "Crimson"),
        new(ThemeType.Cyberpunk, "Cyberpunk")
    ];

    public IReadOnlyList<LabeledValue<QualityPreference>> QualityChoices { get; } =
    [
        new(QualityPreference.Best, "Best available"),
        new(QualityPreference.P2160, "2160p (4K)"),
        new(QualityPreference.P1440, "1440p (2K)"),
        new(QualityPreference.P1080, "1080p"),
        new(QualityPreference.P720, "720p"),
        new(QualityPreference.P480, "480p"),
        new(QualityPreference.P360, "360p")
    ];

    public IReadOnlyList<LabeledValue<OutputContainer>> ContainerChoices { get; } =
    [
        new(OutputContainer.Mp4, "MP4"),
        new(OutputContainer.Mkv, "MKV"),
        new(OutputContainer.Webm, "WebM")
    ];

    public IReadOnlyList<LabeledValue<string>> StretchChoices { get; } =
    [
        new("UniformToFill", "Fill (crop)"),
        new("Uniform", "Fit"),
        new("Fill", "Stretch"),
        new("None", "Center")
    ];

    public int MaxConcurrencyCeiling => AppConstants.MaxAllowedConcurrentDownloads;

    public ObservableCollection<DependencyStatus> Dependencies { get; } = [];

    public ObservableCollection<string> Wallpapers { get; } = [];

    // --- General ---
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _minimizeToTray;
    [ObservableProperty] private bool _checkForUpdates;

    // --- Downloads ---
    [ObservableProperty] private string? _defaultDownloadDirectory;
    [ObservableProperty] private string? _videoDirectory;
    [ObservableProperty] private string? _audioDirectory;
    [ObservableProperty] private string? _thumbnailDirectory;
    [ObservableProperty] private string? _subtitleDirectory;
    [ObservableProperty] private OutputContainer _defaultFormat;
    [ObservableProperty] private QualityPreference _defaultQuality;
    [ObservableProperty] private int _maxConcurrentDownloads;
    [ObservableProperty] private bool _autoStartDownloads;
    [ObservableProperty] private bool _retryFailedDownloads;
    [ObservableProperty] private int _retryCount;
    [ObservableProperty] private bool _createSubfolders;
    [ObservableProperty] private bool _subfolderByChannel;
    [ObservableProperty] private bool _subfolderByPlaylist;
    [ObservableProperty] private bool _subfolderByDate;
    [ObservableProperty] private string _outputTemplate = AppConstants.DefaultOutputTemplate;
    [ObservableProperty] private bool _overwriteExisting;

    // --- Appearance ---
    [ObservableProperty] private ThemeType _theme;
    [ObservableProperty] private string? _accentColor;
    [ObservableProperty] private string? _wallpaperFileName;
    [ObservableProperty] private bool _wallpaperEnabled;
    [ObservableProperty] private double _wallpaperOpacity;
    [ObservableProperty] private double _wallpaperBlur;
    [ObservableProperty] private double _wallpaperDarkness;
    [ObservableProperty] private string _wallpaperStretch = "UniformToFill";
    [ObservableProperty] private bool _animationEnabled;
    [ObservableProperty] private bool _transparencyEnabled;
    [ObservableProperty] private bool _compactMode;

    // --- Tools ---
    [ObservableProperty] private string? _ytDlpPath;
    [ObservableProperty] private string? _ytDlpCustomArguments;
    [ObservableProperty] private bool _ytDlpAutoUpdate;
    [ObservableProperty] private string? _ffmpegPath;
    [ObservableProperty] private string? _ffprobePath;

    // --- Notifications ---
    [ObservableProperty] private bool _notifyStarted;
    [ObservableProperty] private bool _notifyCompleted;
    [ObservableProperty] private bool _notifyFailed;

    [ObservableProperty]
    private bool _isUpdatingYtDlp;

    [ObservableProperty]
    private double _ytDlpUpdateProgress;

    private void LoadFromDraft()
    {
        _loading = true;

        var g = _draft.General;
        StartWithWindows = g.StartWithWindows;
        MinimizeToTray = g.MinimizeToTray;
        CheckForUpdates = g.CheckForUpdates;

        var d = _draft.Downloads;
        DefaultDownloadDirectory = d.DefaultDownloadDirectory;
        VideoDirectory = d.VideoDirectory;
        AudioDirectory = d.AudioDirectory;
        ThumbnailDirectory = d.ThumbnailDirectory;
        SubtitleDirectory = d.SubtitleDirectory;
        DefaultFormat = d.DefaultFormat == OutputContainer.Auto ? OutputContainer.Mp4 : d.DefaultFormat;
        DefaultQuality = d.DefaultQuality;
        MaxConcurrentDownloads = d.MaxConcurrentDownloads;
        AutoStartDownloads = d.AutoStartDownloads;
        RetryFailedDownloads = d.RetryFailedDownloads;
        RetryCount = d.RetryCount;
        CreateSubfolders = d.CreateSubfolders;
        SubfolderByChannel = d.SubfolderByChannel;
        SubfolderByPlaylist = d.SubfolderByPlaylist;
        SubfolderByDate = d.SubfolderByDate;
        OutputTemplate = d.OutputTemplate;
        OverwriteExisting = d.OverwriteExisting;

        var a = _draft.Appearance;
        Theme = a.Theme;
        AccentColor = a.AccentColor;
        WallpaperFileName = a.WallpaperFileName;
        WallpaperEnabled = a.WallpaperEnabled;
        WallpaperOpacity = a.WallpaperOpacity;
        WallpaperBlur = a.WallpaperBlur;
        WallpaperDarkness = a.WallpaperDarkness;
        WallpaperStretch = a.WallpaperStretch;
        AnimationEnabled = a.AnimationEnabled;
        TransparencyEnabled = a.TransparencyEnabled;
        CompactMode = a.CompactMode;

        YtDlpPath = _draft.YtDlp.ExecutablePath;
        YtDlpCustomArguments = _draft.YtDlp.CustomArguments;
        YtDlpAutoUpdate = _draft.YtDlp.AutoUpdate;
        FfmpegPath = _draft.FFmpeg.ExecutablePath;
        FfprobePath = _draft.FFmpeg.FFprobePath;

        NotifyStarted = _draft.Notifications.DownloadStarted;
        NotifyCompleted = _draft.Notifications.DownloadCompleted;
        NotifyFailed = _draft.Notifications.DownloadFailed;

        _loading = false;
    }

    // Live theme/accent application for immediate feedback.
    partial void OnThemeChanged(ThemeType value)
    {
        if (!_loading)
        {
            _themeService.ApplyTheme(value);
        }
    }

    partial void OnAccentColorChanged(string? value)
    {
        if (!_loading)
        {
            _themeService.ApplyAccentColor(string.IsNullOrWhiteSpace(value) ? null : value);
        }
    }

    protected override Task OnFirstActivatedAsync()
    {
        _ = CheckDependenciesAsync();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        WriteToDraft();
        try
        {
            await _settings.SaveAsync(_draft).ConfigureAwait(true);
            _manager.SetMaxConcurrency(MaxConcurrentDownloads);
            _draft = _settings.Current.Clone();
            _notifications.Success("Settings saved.", "Settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
            _notifications.Error("Couldn't save settings.", "Settings");
        }
    }

    [RelayCommand]
    private void Reset()
    {
        _draft = _settings.Current.Clone();
        LoadFromDraft();

        // Re-apply persisted appearance in case live edits diverged.
        _themeService.ApplyTheme(_draft.Appearance.Theme);
        _themeService.ApplyAccentColor(_draft.Appearance.AccentColor);
        _notifications.Info("Reverted unsaved changes.", "Settings");
    }

    private void WriteToDraft()
    {
        _draft.General.StartWithWindows = StartWithWindows;
        _draft.General.MinimizeToTray = MinimizeToTray;
        _draft.General.CheckForUpdates = CheckForUpdates;

        var d = _draft.Downloads;
        d.DefaultDownloadDirectory = Nullify(DefaultDownloadDirectory);
        d.VideoDirectory = Nullify(VideoDirectory);
        d.AudioDirectory = Nullify(AudioDirectory);
        d.ThumbnailDirectory = Nullify(ThumbnailDirectory);
        d.SubtitleDirectory = Nullify(SubtitleDirectory);
        d.DefaultFormat = DefaultFormat;
        d.DefaultQuality = DefaultQuality;
        d.MaxConcurrentDownloads = Math.Clamp(MaxConcurrentDownloads, 1, MaxConcurrencyCeiling);
        d.AutoStartDownloads = AutoStartDownloads;
        d.RetryFailedDownloads = RetryFailedDownloads;
        d.RetryCount = Math.Max(0, RetryCount);
        d.CreateSubfolders = CreateSubfolders;
        d.SubfolderByChannel = SubfolderByChannel;
        d.SubfolderByPlaylist = SubfolderByPlaylist;
        d.SubfolderByDate = SubfolderByDate;
        d.OutputTemplate = string.IsNullOrWhiteSpace(OutputTemplate) ? AppConstants.DefaultOutputTemplate : OutputTemplate;
        d.OverwriteExisting = OverwriteExisting;

        var a = _draft.Appearance;
        a.Theme = Theme;
        a.AccentColor = Nullify(AccentColor);
        a.WallpaperFileName = WallpaperFileName;
        a.WallpaperEnabled = WallpaperEnabled;
        a.WallpaperOpacity = WallpaperOpacity;
        a.WallpaperBlur = WallpaperBlur;
        a.WallpaperDarkness = WallpaperDarkness;
        a.WallpaperStretch = WallpaperStretch;
        a.AnimationEnabled = AnimationEnabled;
        a.TransparencyEnabled = TransparencyEnabled;
        a.CompactMode = CompactMode;

        _draft.YtDlp.ExecutablePath = Nullify(YtDlpPath);
        _draft.YtDlp.CustomArguments = Nullify(YtDlpCustomArguments);
        _draft.YtDlp.AutoUpdate = YtDlpAutoUpdate;
        _draft.FFmpeg.ExecutablePath = Nullify(FfmpegPath);
        _draft.FFmpeg.FFprobePath = Nullify(FfprobePath);

        _draft.Notifications.DownloadStarted = NotifyStarted;
        _draft.Notifications.DownloadCompleted = NotifyCompleted;
        _draft.Notifications.DownloadFailed = NotifyFailed;
    }

    // --- Folder / file pickers ---

    [RelayCommand]
    private void BrowseDefaultDirectory() => Pick(v => DefaultDownloadDirectory = v, DefaultDownloadDirectory);

    [RelayCommand]
    private void BrowseVideoDirectory() => Pick(v => VideoDirectory = v, VideoDirectory);

    [RelayCommand]
    private void BrowseAudioDirectory() => Pick(v => AudioDirectory = v, AudioDirectory);

    [RelayCommand]
    private void BrowseThumbnailDirectory() => Pick(v => ThumbnailDirectory = v, ThumbnailDirectory);

    [RelayCommand]
    private void BrowseSubtitleDirectory() => Pick(v => SubtitleDirectory = v, SubtitleDirectory);

    [RelayCommand]
    private void BrowseYtDlp() => PickExe(v => YtDlpPath = v, YtDlpPath);

    [RelayCommand]
    private void BrowseFfmpeg() => PickExe(v => FfmpegPath = v, FfmpegPath);

    [RelayCommand]
    private void BrowseFfprobe() => PickExe(v => FfprobePath = v, FfprobePath);

    [RelayCommand]
    private void ClearAccent() => AccentColor = null;

    private void Pick(Action<string> assign, string? current)
    {
        var chosen = _dialogs.PickFolder("Choose a folder", current);
        if (chosen is not null)
        {
            assign(chosen);
        }
    }

    private void PickExe(Action<string> assign, string? current)
    {
        var dir = string.IsNullOrWhiteSpace(current) ? null : System.IO.Path.GetDirectoryName(current);
        var chosen = _dialogs.PickFile("Choose an executable", "Executable (*.exe)|*.exe|All files (*.*)|*.*", dir);
        if (chosen is not null)
        {
            assign(chosen);
        }
    }

    // --- Dependencies ---

    [RelayCommand]
    private async Task CheckDependenciesAsync()
    {
        try
        {
            var statuses = await _dependencies.CheckAllAsync().ConfigureAwait(true);
            Dependencies.Clear();
            foreach (var status in statuses)
            {
                Dependencies.Add(status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dependency check failed.");
        }
    }

    private bool CanUpdateYtDlp() => !IsUpdatingYtDlp;

    [RelayCommand(CanExecute = nameof(CanUpdateYtDlp))]
    private async Task UpdateYtDlpAsync()
    {
        IsUpdatingYtDlp = true;
        YtDlpUpdateProgress = 0;
        UpdateYtDlpCommand.NotifyCanExecuteChanged();
        try
        {
            var progress = new Progress<double>(p => YtDlpUpdateProgress = p);
            var path = await _dependencies.UpdateYtDlpAsync(progress).ConfigureAwait(true);
            _notifications.Success("yt-dlp updated.", "Dependencies");
            _logger.LogInformation("yt-dlp updated at {Path}", path);
            await CheckDependenciesAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "yt-dlp update failed.");
            _notifications.Error("Couldn't update yt-dlp.", "Dependencies");
        }
        finally
        {
            IsUpdatingYtDlp = false;
            UpdateYtDlpCommand.NotifyCanExecuteChanged();
        }
    }

    // --- Wallpapers ---

    private void RefreshWallpapers()
    {
        Wallpapers.Clear();
        foreach (var name in _wallpapers.ListWallpapers())
        {
            Wallpapers.Add(name);
        }
    }

    [RelayCommand]
    private async Task ImportWallpaperAsync()
    {
        var chosen = _dialogs.PickFile(
            "Choose a wallpaper image",
            "Images (*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif");
        if (chosen is null)
        {
            return;
        }

        var stored = await _wallpapers.ImportAsync(chosen).ConfigureAwait(true);
        if (stored is null)
        {
            _notifications.Warning("That image couldn't be imported.", "Wallpaper");
            return;
        }

        RefreshWallpapers();
        WallpaperFileName = stored;
        WallpaperEnabled = true;
        _notifications.Success("Wallpaper added.", "Wallpaper");
    }

    [RelayCommand]
    private void SelectWallpaper(string? fileName)
    {
        WallpaperFileName = fileName;
        WallpaperEnabled = !string.IsNullOrWhiteSpace(fileName);
    }

    [RelayCommand]
    private void RemoveWallpaper(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        _wallpapers.Delete(fileName);
        if (string.Equals(WallpaperFileName, fileName, StringComparison.OrdinalIgnoreCase))
        {
            WallpaperFileName = null;
            WallpaperEnabled = false;
        }

        RefreshWallpapers();
    }

    private static string? Nullify(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
