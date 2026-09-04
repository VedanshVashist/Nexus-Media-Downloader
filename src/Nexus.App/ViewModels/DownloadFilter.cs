using Nexus.Core.Enums;

namespace Nexus.App.ViewModels;

/// <summary>Coarse status buckets used to filter the Downloads list.</summary>
public enum DownloadFilter
{
    All,
    Active,
    Completed,
    Failed
}

internal static class DownloadFilterExtensions
{
    /// <summary>True when a task's status belongs to the given filter bucket.</summary>
    public static bool Matches(this DownloadFilter filter, DownloadStatus status) => filter switch
    {
        DownloadFilter.All => true,
        DownloadFilter.Active => status is DownloadStatus.Queued or DownloadStatus.Downloading
            or DownloadStatus.Processing or DownloadStatus.Paused,
        DownloadFilter.Completed => status == DownloadStatus.Completed,
        DownloadFilter.Failed => status is DownloadStatus.Failed or DownloadStatus.Cancelled,
        _ => true
    };
}
