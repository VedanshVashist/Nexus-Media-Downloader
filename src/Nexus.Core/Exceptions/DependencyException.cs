namespace Nexus.Core.Exceptions;

/// <summary>Raised when a required external dependency (yt-dlp, FFmpeg, ffprobe) is missing or invalid.</summary>
public sealed class DependencyException : NexusException
{
    /// <summary>Logical name of the dependency, e.g. "yt-dlp" or "ffmpeg".</summary>
    public string DependencyName { get; }

    public DependencyException(string dependencyName, string message, string? userMessage = null, Exception? innerException = null)
        : base(message, userMessage ?? $"{dependencyName} is not available.", innerException)
    {
        DependencyName = dependencyName;
    }
}
