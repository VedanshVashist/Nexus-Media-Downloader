using Nexus.Core.Enums;

namespace Nexus.Core.DTOs;

/// <summary>
/// An immutable progress snapshot emitted by the download engine through
/// <see cref="IProgress{T}"/>. Parsed from yt-dlp's progress output.
/// </summary>
public sealed record DownloadProgress
{
    /// <summary>The task this snapshot belongs to.</summary>
    public required Guid TaskId { get; init; }

    /// <summary>Current lifecycle status.</summary>
    public DownloadStatus Status { get; init; }

    /// <summary>Percentage 0–100.</summary>
    public double Percent { get; init; }

    public long DownloadedBytes { get; init; }
    public long TotalBytes { get; init; }

    /// <summary>Current speed in bytes per second.</summary>
    public double SpeedBytesPerSecond { get; init; }

    /// <summary>Estimated time remaining.</summary>
    public TimeSpan? Eta { get; init; }

    /// <summary>Optional human-readable phase note, e.g. "Merging formats".</summary>
    public string? Note { get; init; }
}
