using Microsoft.Extensions.Logging;
using Nexus.Core.DTOs;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.Infrastructure.Downloads;

/// <summary>
/// Executes a single download end-to-end by delegating to <see cref="IYtDlpService"/>
/// (which performs fetch + yt-dlp-driven post-processing). Updates the task's
/// observable state and surfaces friendly errors. Queue orchestration is handled
/// by the manager, not here.
/// </summary>
public sealed class DownloadService : IDownloadService
{
    private readonly IYtDlpService _ytDlp;
    private readonly ISettingsService _settings;
    private readonly ILogger<DownloadService> _logger;

    public DownloadService(IYtDlpService ytDlp, ISettingsService settings, ILogger<DownloadService> logger)
    {
        _ytDlp = ytDlp;
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(
        DownloadTask task,
        IProgress<DownloadProgress> progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(progress);

        task.Status = DownloadStatus.Downloading;
        task.StartedAt = DateTimeOffset.UtcNow;
        task.ErrorMessage = null;

        // Forward progress to the task's observable state, then to any listener.
        var relay = new Progress<DownloadProgress>(p =>
        {
            task.Progress = p.Percent;
            task.DownloadedBytes = p.DownloadedBytes;
            task.TotalBytes = p.TotalBytes;
            task.Speed = p.SpeedBytesPerSecond;
            task.Eta = p.Eta;
            if (p.Status == DownloadStatus.Processing && task.Status == DownloadStatus.Downloading)
            {
                task.Status = DownloadStatus.Processing;
            }

            progress.Report(p);
        });

        var outputPath = await _ytDlp.DownloadAsync(task, _settings.Current, relay, cancellationToken)
            .ConfigureAwait(false);

        task.OutputPath = outputPath;
        task.Progress = 100;
        task.Status = DownloadStatus.Completed;
        task.CompletedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation("Download completed for task {TaskId}", task.Id);
        return outputPath;
    }
}
