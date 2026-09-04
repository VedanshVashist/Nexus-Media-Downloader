namespace Nexus.Core.Exceptions;

/// <summary>Raised when FFmpeg/ffprobe fails, is missing, or returns a non-zero exit code.</summary>
public sealed class FFmpegException : NexusException
{
    public int? ExitCode { get; }
    public string? StandardError { get; }

    public FFmpegException(
        string message,
        string? userMessage = null,
        int? exitCode = null,
        string? standardError = null,
        Exception? innerException = null)
        : base(message, userMessage ?? "Media processing failed.", innerException)
    {
        ExitCode = exitCode;
        StandardError = standardError;
    }
}
