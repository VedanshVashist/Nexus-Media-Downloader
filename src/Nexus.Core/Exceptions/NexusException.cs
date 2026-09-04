namespace Nexus.Core.Exceptions;

/// <summary>
/// Base type for all domain exceptions raised by Nexus. Carries a
/// <see cref="UserMessage"/> that is safe to surface directly in the UI, keeping
/// technical detail in <see cref="System.Exception.Message"/> / inner exceptions
/// for the diagnostics/log view.
/// </summary>
public class NexusException : Exception
{
    /// <summary>
    /// A friendly, user-facing message. Never contains stack traces, file paths,
    /// or raw tool output. Defaults to a generic message when not supplied.
    /// </summary>
    public string UserMessage { get; }

    public NexusException(string message, string? userMessage = null, Exception? innerException = null)
        : base(message, innerException)
    {
        UserMessage = userMessage ?? "Something went wrong. Please try again.";
    }
}
