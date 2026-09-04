namespace Nexus.Core.Exceptions;

/// <summary>Raised when application configuration is missing or invalid.</summary>
public sealed class ConfigurationException : NexusException
{
    public ConfigurationException(string message, string? userMessage = null, Exception? innerException = null)
        : base(message, userMessage ?? "There is a problem with the application configuration.", innerException)
    {
    }
}
