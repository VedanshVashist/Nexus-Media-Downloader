namespace Nexus.Core.Exceptions;

/// <summary>Raised when yt-dlp fails to run, returns a non-zero exit code, or emits unparseable output.</summary>
public sealed class YtDlpException : NexusException
{
    /// <summary>The process exit code, when a process actually ran.</summary>
    public int? ExitCode { get; }

    /// <summary>Captured stderr tail, for the diagnostics view. Not shown to normal users.</summary>
    public string? StandardError { get; }

    public YtDlpException(
        string message,
        string? userMessage = null,
        int? exitCode = null,
        string? standardError = null,
        Exception? innerException = null)
        : base(message, userMessage ?? "yt-dlp could not process this request.", innerException)
    {
        ExitCode = exitCode;
        StandardError = standardError;
    }
}
