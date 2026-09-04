using CommunityToolkit.Mvvm.ComponentModel;
using Nexus.Core.Enums;

namespace Nexus.Core.Models;

/// <summary>
/// A live, observable unit of work tracked by the download engine and bound
/// directly to the UI. Progress-related fields raise change notifications so
/// download cards animate without polling.
/// </summary>
/// <remarks>
/// This is domain state that the UI observes; it deliberately derives from
/// <see cref="ObservableObject"/> so the engine can mutate it on a background
/// thread and the UI stays in sync. Persistence uses a separate flat entity.
/// </remarks>
public sealed partial class DownloadTask : ObservableObject
{
    /// <summary>Stable identifier for this task, also used as the persistence key.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Source URL. Treated as untrusted input everywhere downstream.</summary>
    public required string Url { get; init; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _thumbnailUrl;

    /// <summary>Local cached thumbnail path once available, for offline display.</summary>
    [ObservableProperty]
    private string? _thumbnailPath;

    /// <summary>Final output path once known/completed.</summary>
    [ObservableProperty]
    private string? _outputPath;

    /// <summary>The chosen format, if the user picked a specific one.</summary>
    public VideoFormat? SelectedFormat { get; set; }

    /// <summary>yt-dlp format selector string actually used (for diagnostics/history).</summary>
    public string? FormatSelector { get; set; }

    public DownloadType DownloadType { get; init; } = DownloadType.Video;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    [NotifyPropertyChangedFor(nameof(CanRetry))]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    [NotifyPropertyChangedFor(nameof(IsPaused))]
    [NotifyPropertyChangedFor(nameof(IsCompleted))]
    [NotifyPropertyChangedFor(nameof(HasFailed))]
    private DownloadStatus _status = DownloadStatus.Created;

    /// <summary>Progress from 0 to 100.</summary>
    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private long _downloadedBytes;

    [ObservableProperty]
    private long _totalBytes;

    /// <summary>Current speed in bytes/sec, when downloading.</summary>
    [ObservableProperty]
    private double _speed;

    /// <summary>Estimated time remaining, when known.</summary>
    [ObservableProperty]
    private TimeSpan? _eta;

    /// <summary>Friendly error message when <see cref="Status"/> is Failed.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Number of times this task has been retried.</summary>
    [ObservableProperty]
    private int _retryAttempts;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private DateTimeOffset? _startedAt;

    [ObservableProperty]
    private DateTimeOffset? _completedAt;

    /// <summary>Options captured at enqueue time (subtitles, thumbnail, embed flags, etc.).</summary>
    public DownloadOptions Options { get; init; } = new();

    /// <summary>True when the task is in a state the user can cancel.</summary>
    public bool CanCancel => Status is DownloadStatus.Queued or DownloadStatus.Downloading
        or DownloadStatus.Processing or DownloadStatus.Paused;

    /// <summary>True when the task can be retried.</summary>
    public bool CanRetry => Status is DownloadStatus.Failed or DownloadStatus.Cancelled;

    /// <summary>True while the task is queued or working (queued, downloading, or post-processing).</summary>
    public bool IsActive => Status is DownloadStatus.Queued or DownloadStatus.Downloading
        or DownloadStatus.Processing;

    /// <summary>True when the task is paused.</summary>
    public bool IsPaused => Status == DownloadStatus.Paused;

    /// <summary>True when the task finished successfully.</summary>
    public bool IsCompleted => Status == DownloadStatus.Completed;

    /// <summary>True when the task ended without success (failed or cancelled).</summary>
    public bool HasFailed => Status is DownloadStatus.Failed or DownloadStatus.Cancelled;
}
