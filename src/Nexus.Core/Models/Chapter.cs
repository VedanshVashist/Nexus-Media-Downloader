namespace Nexus.Core.Models;

/// <summary>A named time range within a video, as reported by yt-dlp.</summary>
public sealed record Chapter
{
    public required string Title { get; init; }

    /// <summary>Start offset from the beginning of the media.</summary>
    public TimeSpan StartTime { get; init; }

    /// <summary>End offset. May equal <see cref="StartTime"/> when unknown.</summary>
    public TimeSpan EndTime { get; init; }

    /// <summary>Chapter length. Never negative.</summary>
    public TimeSpan Duration => EndTime > StartTime ? EndTime - StartTime : TimeSpan.Zero;
}
