namespace Nexus.Core.Models;

/// <summary>
/// Metadata for a playlist as reported by yt-dlp. Entries may be "flat"
/// (id/title/url only) when extracted with <c>--flat-playlist</c> for speed.
/// </summary>
public sealed record PlaylistInfo
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Uploader { get; init; }
    public string? ChannelId { get; init; }
    public string? WebpageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }

    /// <summary>Items in playlist order.</summary>
    public IReadOnlyList<PlaylistEntry> Entries { get; init; } = [];

    public int Count => Entries.Count;
}

/// <summary>A single entry within a playlist.</summary>
public sealed record PlaylistEntry
{
    /// <summary>1-based position within the playlist.</summary>
    public int Index { get; init; }

    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Url { get; init; }
    public string? ThumbnailUrl { get; init; }
    public TimeSpan? Duration { get; init; }
}
