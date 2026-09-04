namespace Nexus.Core.Interfaces;

/// <summary>Severity levels for in-app toast notifications.</summary>
public enum NotificationLevel
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>A single toast notification.</summary>
public sealed record Notification
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Message { get; init; }
    public NotificationLevel Level { get; init; } = NotificationLevel.Info;
    public string? Title { get; init; }

    /// <summary>How long the toast stays before auto-dismiss.</summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(4);
}

/// <summary>
/// In-app notification/toast system. Deliberately independent of MessageBox so
/// the UI can render non-blocking toasts.
/// </summary>
public interface INotificationService
{
    /// <summary>Raised when a new notification is published.</summary>
    event EventHandler<Notification>? NotificationPublished;

    void Notify(Notification notification);

    void Info(string message, string? title = null);
    void Success(string message, string? title = null);
    void Warning(string message, string? title = null);
    void Error(string message, string? title = null);
}
