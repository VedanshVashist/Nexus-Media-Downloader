using System.Collections.ObjectModel;
using Nexus.Core.Models;

namespace Nexus.Core.Interfaces;

/// <summary>
/// Tracks all known download tasks, enforces the concurrency limit, and exposes
/// live collections the UI can bind to. Bridges the queue and the executing
/// downloads.
/// </summary>
public interface IDownloadManager
{
    /// <summary>All tasks currently tracked (active, queued, and recently finished in-session).</summary>
    ReadOnlyObservableCollection<DownloadTask> Tasks { get; }

    /// <summary>Raised when a task's status changes, for notification/history hooks.</summary>
    event EventHandler<DownloadTask>? TaskStatusChanged;

    /// <summary>Adds a task and starts it when a slot is free (respecting auto-start settings).</summary>
    Task EnqueueAsync(DownloadTask task, CancellationToken cancellationToken = default);

    /// <summary>Requests cancellation of a running or queued task.</summary>
    void Cancel(Guid taskId);

    /// <summary>Pauses a task when technically supported; otherwise a no-op returning false.</summary>
    bool TryPause(Guid taskId);

    /// <summary>Resumes a paused task when supported.</summary>
    Task<bool> TryResumeAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Retries a failed or cancelled task.</summary>
    Task RetryAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Removes a task from tracking (does not delete downloaded files).</summary>
    void Remove(Guid taskId);

    /// <summary>Updates the maximum number of concurrent downloads at runtime.</summary>
    void SetMaxConcurrency(int maxConcurrent);
}
