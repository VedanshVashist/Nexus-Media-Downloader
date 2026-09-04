using Nexus.Core.Enums;

namespace Nexus.Core.Models;

/// <summary>
/// A persisted record of a completed (or attempted) download. Plain POCO: EF Core
/// mapping is configured via Fluent API in Infrastructure, keeping Core free of
/// persistence attributes. Only metadata and paths are stored — never media.
/// </summary>
public sealed class HistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ThumbnailPath { get; set; }

    /// <summary>Absolute path to the downloaded file, when it completed.</summary>
    public string? FilePath { get; set; }

    public DownloadType DownloadType { get; set; }
    public DownloadStatus Status { get; set; }

    /// <summary>Container/extension actually produced, e.g. "mp4".</summary>
    public string? Format { get; set; }

    /// <summary>Quality label, e.g. "1080p" or "best".</summary>
    public string? Quality { get; set; }

    public long? FileSizeBytes { get; set; }

    public DateTimeOffset DownloadedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsFavorite { get; set; }
}
