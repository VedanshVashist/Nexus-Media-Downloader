namespace Nexus.Core.DTOs;

/// <summary>Result of probing for an external tool (yt-dlp, ffmpeg, ffprobe).</summary>
public sealed record DependencyStatus
{
    /// <summary>Logical dependency name, e.g. "yt-dlp".</summary>
    public required string Name { get; init; }

    /// <summary>True when a usable executable was found and validated.</summary>
    public bool IsAvailable { get; init; }

    /// <summary>Resolved absolute path to the executable, when found.</summary>
    public string? Path { get; init; }

    /// <summary>Detected version string, when it could be read.</summary>
    public string? Version { get; init; }

    /// <summary>Diagnostic detail when unavailable. Safe to show in the diagnostics view.</summary>
    public string? Detail { get; init; }
}
