namespace Nexus.Core.Exceptions;

/// <summary>Raised when a supplied URL is malformed or unsupported.</summary>
public sealed class InvalidUrlException : NexusException
{
    /// <summary>The offending URL, for logging. May be null when input was empty.</summary>
    public string? Url { get; }

    public InvalidUrlException(string? url, string? userMessage = null, Exception? innerException = null)
        : base($"Invalid or unsupported URL: '{url}'.", userMessage ?? "That doesn't look like a valid link.", innerException)
    {
        Url = url;
    }
}
