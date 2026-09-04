namespace Nexus.Core.Exceptions;

/// <summary>Raised when a persistence operation fails.</summary>
public sealed class DatabaseException : NexusException
{
    public DatabaseException(string message, string? userMessage = null, Exception? innerException = null)
        : base(message, userMessage ?? "Could not access application data.", innerException)
    {
    }
}
