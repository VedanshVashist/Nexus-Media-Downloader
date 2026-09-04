using System.IO;
using Microsoft.Extensions.Logging;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.App.Services;

/// <summary>
/// Listens to the download manager and persists a <see cref="HistoryEntry"/> when a
/// task reaches a terminal state. Keeps history-writing out of the view-models and
/// guarantees each task is recorded at most once per completion.
/// </summary>
public sealed class HistoryRecorder : IDisposable
{
    private readonly IDownloadManager _manager;
    private readonly IHistoryRepository _history;
    private readonly IFavoritesRepository _favorites;
    private readonly ILogger<HistoryRecorder> _logger;

    private readonly HashSet<Guid> _recorded = [];
    private readonly object _gate = new();
    private bool _started;

    public HistoryRecorder(
        IDownloadManager manager,
        IHistoryRepository history,
        IFavoritesRepository favorites,
        ILogger<HistoryRecorder> logger)
    {
        _manager = manager;
        _history = history;
        _favorites = favorites;
        _logger = logger;
    }

    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _manager.TaskStatusChanged += OnTaskStatusChanged;
    }

    private void OnTaskStatusChanged(object? sender, DownloadTask task)
    {
        if (task.Status is not (DownloadStatus.Completed or DownloadStatus.Failed))
        {
            return;
        }

        lock (_gate)
        {
            // Record a completion once; a later failure/re-completion after retry
            // is allowed to record again only if it previously failed.
            if (!_recorded.Add(task.Id) && task.Status == DownloadStatus.Failed)
            {
                return;
            }
        }

        _ = RecordAsync(task);
    }

    private async Task RecordAsync(DownloadTask task)
    {
        try
        {
            var isFavorite = await _favorites.ExistsAsync(task.Url).ConfigureAwait(false);

            var entry = new HistoryEntry
            {
                Url = task.Url,
                Title = string.IsNullOrWhiteSpace(task.Title) ? task.Url : task.Title,
                ThumbnailUrl = task.ThumbnailUrl,
                ThumbnailPath = task.ThumbnailPath,
                FilePath = task.OutputPath,
                DownloadType = task.DownloadType,
                Status = task.Status,
                Format = ResolveFormat(task),
                Quality = task.Options.Quality == QualityPreference.Custom
                    ? task.SelectedFormat?.FormatNote ?? "custom"
                    : task.Options.Quality.ToString(),
                FileSizeBytes = ResolveSize(task),
                DownloadedAt = task.CompletedAt ?? DateTimeOffset.UtcNow,
                IsFavorite = isFavorite
            };

            await _history.AddAsync(entry).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record history for task {TaskId}.", task.Id);
        }
    }

    private static string? ResolveFormat(DownloadTask task)
    {
        if (!string.IsNullOrWhiteSpace(task.OutputPath))
        {
            var ext = Path.GetExtension(task.OutputPath);
            if (!string.IsNullOrWhiteSpace(ext))
            {
                return ext.TrimStart('.').ToLowerInvariant();
            }
        }

        return task.SelectedFormat?.Extension;
    }

    private static long? ResolveSize(DownloadTask task)
    {
        if (task.TotalBytes > 0)
        {
            return task.TotalBytes;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(task.OutputPath) && File.Exists(task.OutputPath))
            {
                return new FileInfo(task.OutputPath).Length;
            }
        }
        catch (IOException)
        {
            // Best-effort size; ignore IO failures.
        }

        return null;
    }

    public void Dispose()
    {
        if (_started)
        {
            _manager.TaskStatusChanged -= OnTaskStatusChanged;
            _started = false;
        }
    }
}
