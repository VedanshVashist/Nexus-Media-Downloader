using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nexus.App.Services;
using Nexus.Core.Enums;
using Nexus.Core.Exceptions;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using Nexus.Core.Utilities;
using Nexus.Infrastructure.Settings;

namespace Nexus.App.ViewModels;

/// <summary>
/// The Home page: paste/analyze a URL, review the resolved video or playlist,
/// choose format/quality and content options, then download now or add to the
/// queue. All URL input is validated before it reaches yt-dlp.
/// </summary>
public sealed partial class HomeViewModel : PageViewModel
{
    private static readonly IReadOnlyList<LabeledValue<QualityPreference>> AllQualities =
    [
        new(QualityPreference.Best, "Best available"),
        new(QualityPreference.P2160, "2160p (4K)"),
        new(QualityPreference.P1440, "1440p (2K)"),
        new(QualityPreference.P1080, "1080p"),
        new(QualityPreference.P720, "720p"),
        new(QualityPreference.P480, "480p"),
        new(QualityPreference.P360, "360p")
    ];

    private static readonly IReadOnlyList<LabeledValue<OutputContainer>> VideoContainers =
    [
        new(OutputContainer.Auto, "Auto (keep source)"),
        new(OutputContainer.Mp4, "MP4"),
        new(OutputContainer.Mkv, "MKV"),
        new(OutputContainer.Webm, "WebM")
    ];

    private static readonly IReadOnlyList<LabeledValue<OutputContainer>> AudioContainers =
    [
        new(OutputContainer.Mp3, "MP3"),
        new(OutputContainer.M4a, "M4A (AAC)"),
        new(OutputContainer.Opus, "Opus"),
        new(OutputContainer.Flac, "FLAC"),
        new(OutputContainer.Wav, "WAV"),
        new(OutputContainer.Auto, "Auto (keep source)")
    ];

    private readonly IYtDlpService _ytDlp;
    private readonly IDownloadManager _manager;
    private readonly IQueueService _queue;
    private readonly IFavoritesRepository _favorites;
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notifications;
    private readonly ILogger<HomeViewModel> _logger;

    /// <summary>The live options template that new tasks are cloned from.</summary>
    private readonly DownloadOptions _options = new();

    private CancellationTokenSource? _analyzeCts;

    public HomeViewModel(
        IYtDlpService ytDlp,
        IDownloadManager manager,
        IQueueService queue,
        IFavoritesRepository favorites,
        ISettingsService settings,
        IDialogService dialogs,
        INotificationService notifications,
        ILogger<HomeViewModel> logger)
        : base("home", "Home", NavGlyph.Home)
    {
        _ytDlp = ytDlp;
        _manager = manager;
        _queue = queue;
        _favorites = favorites;
        _settings = settings;
        _dialogs = dialogs;
        _notifications = notifications;
        _logger = logger;

        var downloads = settings.Current.Downloads;
        _selectedQuality = downloads.DefaultQuality == QualityPreference.Custom
            ? QualityPreference.Best
            : downloads.DefaultQuality;
        _selectedContainer = downloads.DefaultFormat;
        _outputDirectory = downloads.DefaultDownloadDirectory ?? AppPaths.DefaultDownloadDirectory();
        _containerChoices = VideoContainers;

        _options.Quality = _selectedQuality;
        _options.Container = _selectedContainer;
        _options.OutputDirectory = _outputDirectory;
    }

    /// <summary>Raised when the user starts downloads, so the shell can switch to Downloads.</summary>
    public event Action? NavigateToDownloadsRequested;

    /// <summary>Raised when the user queues work, so the shell can switch to the Queue page.</summary>
    public event Action? NavigateToQueueRequested;

    public IReadOnlyList<LabeledValue<QualityPreference>> QualityChoices => AllQualities;

    public ObservableCollection<FormatOptionViewModel> Formats { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    [NotifyCanExecuteChangedFor(nameof(PasteAndAnalyzeCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    private bool _isAnalyzing;

    [ObservableProperty]
    private string? _analysisError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVideo))]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddToQueueCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddToFavoritesCommand))]
    private VideoInfo? _video;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlaylist))]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddToQueueCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddToFavoritesCommand))]
    private PlaylistInfo? _playlist;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsQualityAuto))]
    private FormatOptionViewModel? _selectedFormat;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAudio))]
    private DownloadType _selectedDownloadType = DownloadType.Video;

    [ObservableProperty]
    private QualityPreference _selectedQuality;

    [ObservableProperty]
    private OutputContainer _selectedContainer;

    [ObservableProperty]
    private IReadOnlyList<LabeledValue<OutputContainer>> _containerChoices;

    [ObservableProperty]
    private string? _outputDirectory;

    [ObservableProperty]
    private bool _downloadThumbnail;

    [ObservableProperty]
    private bool _embedThumbnail = true;

    [ObservableProperty]
    private bool _downloadSubtitles;

    [ObservableProperty]
    private bool _downloadAutomaticSubtitles;

    [ObservableProperty]
    private bool _embedSubtitles;

    [ObservableProperty]
    private string _subtitleLanguages = string.Empty;

    [ObservableProperty]
    private bool _embedMetadata = true;

    [ObservableProperty]
    private bool _embedChapters = true;

    [ObservableProperty]
    private bool _writeInfoJson;

    public bool HasVideo => Video is not null;
    public bool HasPlaylist => Playlist is not null;
    public bool HasResult => Video is not null || Playlist is not null;
    public bool IsAudio => SelectedDownloadType == DownloadType.Audio;
    public bool IsQualityAuto => SelectedFormat is null;

    // --- Option synchronization into the live template ---

    partial void OnSelectedDownloadTypeChanged(DownloadType value)
    {
        _options.DownloadType = value;
        ContainerChoices = value == DownloadType.Audio ? AudioContainers : VideoContainers;

        // Keep the selected container valid for the new type.
        if (ContainerChoices.All(c => c.Value != SelectedContainer))
        {
            SelectedContainer = ContainerChoices[0].Value;
        }

        // Format list is type-specific; clear any manual pick.
        SelectedFormat = null;
        PopulateFormats();
    }

    partial void OnSelectedQualityChanged(QualityPreference value) => _options.Quality = value;

    partial void OnSelectedContainerChanged(OutputContainer value) => _options.Container = value;

    partial void OnOutputDirectoryChanged(string? value) =>
        _options.OutputDirectory = string.IsNullOrWhiteSpace(value) ? null : value;

    partial void OnDownloadThumbnailChanged(bool value) => _options.DownloadThumbnail = value;
    partial void OnEmbedThumbnailChanged(bool value) => _options.EmbedThumbnail = value;
    partial void OnDownloadSubtitlesChanged(bool value) => _options.DownloadSubtitles = value;
    partial void OnDownloadAutomaticSubtitlesChanged(bool value) => _options.DownloadAutomaticSubtitles = value;
    partial void OnEmbedSubtitlesChanged(bool value) => _options.EmbedSubtitles = value;
    partial void OnEmbedMetadataChanged(bool value) => _options.EmbedMetadata = value;
    partial void OnEmbedChaptersChanged(bool value) => _options.EmbedChapters = value;
    partial void OnWriteInfoJsonChanged(bool value) => _options.WriteInfoJson = value;

    partial void OnSubtitleLanguagesChanged(string value) =>
        _options.SubtitleLanguages = value
            .Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // --- Commands ---

    private bool CanAnalyze() => !IsAnalyzing && !string.IsNullOrWhiteSpace(Url);

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        if (!UrlValidator.IsValid(Url, out var normalized))
        {
            AnalysisError = "Enter a valid http(s) link.";
            return;
        }

        _analyzeCts?.Cancel();
        _analyzeCts = new CancellationTokenSource();
        var token = _analyzeCts.Token;

        IsAnalyzing = true;
        IsBusy = true;
        AnalysisError = null;

        try
        {
            var result = await _ytDlp.AnalyzeAsync(normalized, token).ConfigureAwait(true);
            if (token.IsCancellationRequested)
            {
                return;
            }

            Url = normalized;
            if (result.IsPlaylist)
            {
                Playlist = result.Playlist;
                Video = null;
                Formats.Clear();
                SelectedFormat = null;
            }
            else
            {
                Video = result.Video;
                Playlist = null;
                PopulateFormats();
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer analysis; ignore.
        }
        catch (NexusException ex)
        {
            AnalysisError = ex.UserMessage;
            _notifications.Error(ex.UserMessage, "Analyze");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error analyzing URL.");
            AnalysisError = "Something went wrong reading that link.";
            _notifications.Error(AnalysisError, "Analyze");
        }
        finally
        {
            IsAnalyzing = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Paste()
    {
        var text = TryGetClipboardText();
        if (!string.IsNullOrWhiteSpace(text))
        {
            Url = text.Trim();
        }
    }

    private bool CanPasteAndAnalyze() => !IsAnalyzing;

    [RelayCommand(CanExecute = nameof(CanPasteAndAnalyze))]
    private async Task PasteAndAnalyzeAsync()
    {
        var text = TryGetClipboardText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Url = text.Trim();
        await AnalyzeAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private void Clear()
    {
        _analyzeCts?.Cancel();
        Url = string.Empty;
        Video = null;
        Playlist = null;
        Formats.Clear();
        SelectedFormat = null;
        AnalysisError = null;
    }

    [RelayCommand]
    private void BrowseOutput()
    {
        var chosen = _dialogs.PickFolder("Choose a download folder", OutputDirectory);
        if (chosen is not null)
        {
            OutputDirectory = chosen;
        }
    }

    [RelayCommand]
    private void UseQualityPreset() => SelectedFormat = null;

    private bool CanCompose() => HasResult;

    [RelayCommand(CanExecute = nameof(CanCompose))]
    private async Task DownloadAsync()
    {
        var tasks = BuildTasks();
        if (tasks.Count == 0)
        {
            return;
        }

        foreach (var task in tasks)
        {
            await _manager.EnqueueAsync(task).ConfigureAwait(true);
        }

        _notifications.Success(
            tasks.Count == 1 ? "Download started." : $"Started {tasks.Count} downloads.",
            "Nexus");
        NavigateToDownloadsRequested?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanCompose))]
    private void AddToQueue()
    {
        var tasks = BuildTasks();
        if (tasks.Count == 0)
        {
            return;
        }

        _queue.AddRange(tasks);
        _notifications.Info(
            tasks.Count == 1 ? "Added to queue." : $"Added {tasks.Count} items to the queue.",
            "Queue");
        NavigateToQueueRequested?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanCompose))]
    private async Task AddToFavoritesAsync()
    {
        var url = Video?.WebpageUrl ?? Playlist?.WebpageUrl ?? Url;
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (await _favorites.ExistsAsync(url).ConfigureAwait(true))
        {
            _notifications.Info("Already in favorites.", "Favorites");
            return;
        }

        var entry = new FavoriteEntry
        {
            Url = url,
            Title = Video?.Title ?? Playlist?.Title ?? url,
            Channel = Video?.Uploader ?? Playlist?.Uploader,
            ThumbnailUrl = Video?.ThumbnailUrl ?? Playlist?.ThumbnailUrl
        };

        await _favorites.AddAsync(entry).ConfigureAwait(true);
        _notifications.Success("Saved to favorites.", "Favorites");
    }

    private List<DownloadTask> BuildTasks()
    {
        var tasks = new List<DownloadTask>();

        if (Video is not null)
        {
            var url = Video.WebpageUrl ?? Video.OriginalUrl ?? Url;
            tasks.Add(DownloadTaskFactory.Create(
                url, _options, Video.Title, Video.ThumbnailUrl, SelectedFormat?.Format));
        }
        else if (Playlist is not null)
        {
            foreach (var entry in Playlist.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Url))
                {
                    continue;
                }

                tasks.Add(DownloadTaskFactory.Create(
                    entry.Url, _options, entry.Title, entry.ThumbnailUrl));
            }
        }

        return tasks;
    }

    private void PopulateFormats()
    {
        Formats.Clear();
        if (Video is null)
        {
            return;
        }

        var source = SelectedDownloadType == DownloadType.Audio
            ? FormatFilter.AudioFormats(Video.Formats)
            : FormatFilter.VideoFormats(Video.Formats);

        foreach (var format in source)
        {
            Formats.Add(new FormatOptionViewModel(format));
        }
    }

    private static string? TryGetClipboardText()
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
