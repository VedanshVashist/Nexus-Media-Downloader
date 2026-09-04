using Nexus.Core.Enums;
using Nexus.Core.Models;

namespace Nexus.App.Services;

/// <summary>
/// Builds <see cref="DownloadTask"/> instances from the Home page's current choices.
/// Centralized so Home, Queue, History re-download, and Favorites all produce tasks
/// with identical, correct wiring (independent options snapshot, title/thumbnail,
/// selected format).
/// </summary>
public static class DownloadTaskFactory
{
    /// <summary>
    /// Creates a task for <paramref name="url"/> using a private copy of
    /// <paramref name="options"/> so subsequent UI edits never affect this task.
    /// </summary>
    public static DownloadTask Create(
        string url,
        DownloadOptions options,
        string? title = null,
        string? thumbnailUrl = null,
        VideoFormat? selectedFormat = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(options);

        var snapshot = options.Clone();

        // When a specific stream was chosen, reflect it in both the task and options.
        if (selectedFormat is not null)
        {
            snapshot.Quality = QualityPreference.Custom;
            snapshot.CustomFormatId = selectedFormat.FormatId;
        }

        return new DownloadTask
        {
            Url = url,
            DownloadType = snapshot.DownloadType,
            Options = snapshot,
            SelectedFormat = selectedFormat,
            Title = string.IsNullOrWhiteSpace(title) ? url : title!,
            ThumbnailUrl = thumbnailUrl
        };
    }
}
