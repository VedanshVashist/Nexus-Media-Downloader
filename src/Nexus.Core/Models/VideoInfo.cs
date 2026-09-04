namespace Nexus.Core.Models;

/// <summary>
/// Strongly typed metadata for a single media item, mapped from yt-dlp's JSON
/// output. This is the application's own model — UI and services depend on it,
/// never on yt-dlp's raw schema.
/// </summary>
public sealed record VideoInfo
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Uploader { get; init; }
    public string? ChannelId { get; init; }
    public string? ChannelUrl { get; init; }

    /// <summary>Total duration. Zero when unknown (e.g. live streams).</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Upload date at day precision, when yt-dlp reports it.</summary>
    public DateOnly? UploadDate { get; init; }

    public long? ViewCount { get; init; }
    public long? LikeCount { get; init; }

    /// <summary>Best thumbnail URL chosen during mapping.</summary>
    public string? ThumbnailUrl { get; init; }

    public string? WebpageUrl { get; init; }
    public string? OriginalUrl { get; init; }

    public IReadOnlyList<string> Categories { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<Chapter> Chapters { get; init; } = [];
    public IReadOnlyList<VideoFormat> Formats { get; init; } = [];
    public IReadOnlyList<SubtitleInfo> Subtitles { get; init; } = [];

    /// <summary>Primary content language code, when known.</summary>
    public string? Language { get; init; }

    /// <summary>Availability note from yt-dlp, e.g. "public", "unlisted".</summary>
    public string? Availability { get; init; }

    /// <summary>True when the source is currently a live stream.</summary>
    public bool IsLive { get; init; }

    public bool HasChapters => Chapters.Count > 0;
    public bool HasSubtitles => Subtitles.Count > 0;
}
