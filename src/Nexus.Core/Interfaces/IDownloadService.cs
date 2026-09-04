using Nexus.Core.DTOs;
using Nexus.Core.Models;

namespace Nexus.Core.Interfaces;

/// <summary>
/// Executes a single download end-to-end (fetch + post-process), reporting
/// progress and honoring cancellation. Stateless with respect to queueing —
/// orchestration is the manager's concern.
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Runs the download described by <paramref name="task"/>. Mutates the task's
    /// observable state as it progresses and returns the final output path.
    /// </summary>
    Task<string> ExecuteAsync(
        DownloadTask task,
        IProgress<DownloadProgress> progress,
        CancellationToken cancellationToken = default);
}
