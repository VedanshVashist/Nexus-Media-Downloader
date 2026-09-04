namespace Nexus.Core.Enums;

/// <summary>
/// Lifecycle states for a single download task. Transitions are validated by the
/// download engine; see <c>DownloadStatusExtensions</c> for allowed moves.
/// </summary>
public enum DownloadStatus
{
    /// <summary>Created but not yet queued or started.</summary>
    Created = 0,

    /// <summary>Waiting in the queue for a free download slot.</summary>
    Queued = 1,

    /// <summary>Actively downloading.</summary>
    Downloading = 2,

    /// <summary>Post-processing (merge, extract, embed) via FFmpeg.</summary>
    Processing = 3,

    /// <summary>Temporarily paused by the user (only when technically supported).</summary>
    Paused = 4,

    /// <summary>Finished successfully.</summary>
    Completed = 5,

    /// <summary>Failed with an error. Eligible for retry.</summary>
    Failed = 6,

    /// <summary>Cancelled by the user.</summary>
    Cancelled = 7
}
