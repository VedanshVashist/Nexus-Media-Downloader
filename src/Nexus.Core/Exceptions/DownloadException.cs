namespace Nexus.Core.Exceptions;

/// <summary>Raised when a download task fails for reasons other than a direct tool error.</summary>
public sealed class DownloadException : NexusException
{
    public DownloadException(string message, string? userMessage = null, Exception? innerException = null)
        : base(message, userMessage ?? "The download could not be completed.", innerException)
    {
    }
}
