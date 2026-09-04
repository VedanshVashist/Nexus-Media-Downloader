using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Nexus.Core.Constants;
using Nexus.Core.DTOs;
using Nexus.Core.Enums;
using Nexus.Core.Exceptions;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.Infrastructure.Downloads;

/// <summary>
/// Tracks all download tasks and enforces the concurrency limit using a
/// <see cref="SemaphoreSlim"/>. Each task runs on its own background operation
/// with a dedicated <see cref="CancellationTokenSource"/>, so tasks can be
/// cancelled individually without blocking the UI thread.
/// </summary>
/// <remarks>
/// Pause/resume: yt-dlp does not expose reliable mid-stream pausing, so a pause is
/// modeled as a cancel that leaves the task in a resumable state; resume restarts
/// it (yt-dlp continues partial downloads via its <c>.part</c> files).
/// </remarks>
public sealed class DownloadManager : IDownloadManager, IDisposable
{
    private readonly IDownloadService _downloadService;
    private readonly ISettingsService _settings;
    private readonly INotificationService _notifications;
    private readonly ILogger<DownloadManager> _logger;

    private readonly ObservableCollection<DownloadTask> _tasks = [];
    private readonly ReadOnlyObservableCollection<DownloadTask> _readOnlyTasks;
    private readonly Dictionary<Guid, CancellationTokenSource> _cts = [];
    private readonly HashSet<Guid> _pausedByUser = [];
    private readonly object _gate = new();

    private SemaphoreSlim _slots;
    private int _maxConcurrency;

    public DownloadManager(
        IDownloadService downloadService,
        ISettingsService settings,
        INotificationService notifications,
        ILogger<DownloadManager> logger)
    {
        _downloadService = downloadService;
        _settings = settings;
        _notifications = notifications;
        _logger = logger;
        _readOnlyTasks = new ReadOnlyObservableCollection<DownloadTask>(_tasks);

        _maxConcurrency = NormalizeConcurrency(settings.Current.Downloads.MaxConcurrentDownloads);
        _slots = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
    }

    public ReadOnlyObservableCollection<DownloadTask> Tasks => _readOnlyTasks;

    public event EventHandler<DownloadTask>? TaskStatusChanged;

    public Task EnqueueAsync(DownloadTask task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        lock (_gate)
        {
            if (!_tasks.Contains(task))
            {
                _tasks.Add(task);
            }
        }

        task.Status = DownloadStatus.Queued;
        RaiseStatusChanged(task);

        // Fire-and-forget the run loop; it self-throttles on the semaphore. We do
        // not await here so the caller (UI) returns immediately.
        _ = RunAsync(task);
        return Task.CompletedTask;
    }

    public void Cancel(Guid taskId)
    {
        CancellationTokenSource? cts;
        lock (_gate)
        {
            _cts.TryGetValue(taskId, out cts);
            _pausedByUser.Remove(taskId);
        }

        cts?.Cancel();

        var task = FindTask(taskId);
        if (task is not null && task.Status is DownloadStatus.Queued or DownloadStatus.Created)
        {
            // Not yet running: mark cancelled directly.
            task.Status = DownloadStatus.Cancelled;
            RaiseStatusChanged(task);
        }
    }

    public bool TryPause(Guid taskId)
    {
        var task = FindTask(taskId);
        if (task is null || task.Status != DownloadStatus.Downloading)
        {
            return false;
        }

        CancellationTokenSource? cts;
        lock (_gate)
        {
            _pausedByUser.Add(taskId);
            _cts.TryGetValue(taskId, out cts);
        }

        cts?.Cancel();
        return true;
    }

    public Task<bool> TryResumeAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = FindTask(taskId);
        if (task is null || task.Status != DownloadStatus.Paused)
        {
            return Task.FromResult(false);
        }

        lock (_gate)
        {
            _pausedByUser.Remove(taskId);
        }

        task.Status = DownloadStatus.Queued;
        RaiseStatusChanged(task);
        _ = RunAsync(task);
        return Task.FromResult(true);
    }

    public async Task RetryAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = FindTask(taskId);
        if (task is null || !task.CanRetry)
        {
            return;
        }

        task.RetryAttempts++;
        task.Progress = 0;
        task.ErrorMessage = null;
        task.Status = DownloadStatus.Queued;
        RaiseStatusChanged(task);
        await RunAsync(task).ConfigureAwait(false);
    }

    public void Remove(Guid taskId)
    {
        Cancel(taskId);
        lock (_gate)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task is not null)
            {
                _tasks.Remove(task);
            }

            _cts.Remove(taskId);
            _pausedByUser.Remove(taskId);
        }
    }

    public void SetMaxConcurrency(int maxConcurrent)
    {
        var normalized = NormalizeConcurrency(maxConcurrent);
        lock (_gate)
        {
            if (normalized == _maxConcurrency)
            {
                return;
            }

            // Replace the semaphore. In-flight tasks already hold their slots and
            // release into the old instance harmlessly; new admissions use the new one.
            _maxConcurrency = normalized;
            _slots = new SemaphoreSlim(normalized, normalized);
        }

        _logger.LogInformation("Max concurrency set to {Max}", normalized);
    }

    private async Task RunAsync(DownloadTask task)
    {
        var cts = new CancellationTokenSource();
        SemaphoreSlim slots;
        lock (_gate)
        {
            _cts[task.Id] = cts;
            slots = _slots;
        }

        try
        {
            await slots.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            HandleCancellation(task);
            return;
        }

        try
        {
            var progress = new Progress<DownloadProgress>();
            NotifyStarted(task);
            RaiseStatusChanged(task);

            await _downloadService.ExecuteAsync(task, progress, cts.Token).ConfigureAwait(false);

            NotifyCompleted(task);
            RaiseStatusChanged(task);
        }
        catch (OperationCanceledException)
        {
            HandleCancellation(task);
        }
        catch (Exception ex)
        {
            task.Status = DownloadStatus.Failed;
            task.ErrorMessage = (ex as NexusException)?.UserMessage ?? "The download failed.";
            _logger.LogError(ex, "Download failed for task {TaskId}", task.Id);
            NotifyFailed(task);
            RaiseStatusChanged(task);

            await MaybeAutoRetryAsync(task).ConfigureAwait(false);
        }
        finally
        {
            slots.Release();
            lock (_gate)
            {
                _cts.Remove(task.Id);
            }
        }
    }

    private void HandleCancellation(DownloadTask task)
    {
        bool paused;
        lock (_gate)
        {
            paused = _pausedByUser.Remove(task.Id);
        }

        task.Status = paused ? DownloadStatus.Paused : DownloadStatus.Cancelled;
        _logger.LogInformation("Task {TaskId} {State}", task.Id, task.Status);
        RaiseStatusChanged(task);
    }

    private async Task MaybeAutoRetryAsync(DownloadTask task)
    {
        var settings = _settings.Current.Downloads;
        if (!settings.RetryFailedDownloads || task.RetryAttempts >= settings.RetryCount)
        {
            return;
        }

        task.RetryAttempts++;
        _logger.LogInformation("Auto-retrying task {TaskId} (attempt {Attempt})", task.Id, task.RetryAttempts);

        // Small backoff before re-queueing.
        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        task.Status = DownloadStatus.Queued;
        task.ErrorMessage = null;
        RaiseStatusChanged(task);
        _ = RunAsync(task);
    }

    private void NotifyStarted(DownloadTask task)
    {
        if (_settings.Current.Notifications.DownloadStarted)
        {
            _notifications.Info($"Started: {task.Title}", "Download");
        }
    }

    private void NotifyCompleted(DownloadTask task)
    {
        if (_settings.Current.Notifications.DownloadCompleted)
        {
            _notifications.Success($"Completed: {task.Title}", "Download");
        }
    }

    private void NotifyFailed(DownloadTask task)
    {
        if (_settings.Current.Notifications.DownloadFailed)
        {
            _notifications.Error(task.ErrorMessage ?? $"Failed: {task.Title}", "Download");
        }
    }

    private DownloadTask? FindTask(Guid id)
    {
        lock (_gate)
        {
            return _tasks.FirstOrDefault(t => t.Id == id);
        }
    }

    private void RaiseStatusChanged(DownloadTask task) => TaskStatusChanged?.Invoke(this, task);

    private static int NormalizeConcurrency(int value) =>
        Math.Clamp(value <= 0 ? AppConstants.DefaultMaxConcurrentDownloads : value, 1, AppConstants.MaxAllowedConcurrentDownloads);

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var cts in _cts.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }

            _cts.Clear();
            _slots.Dispose();
        }
    }
}
