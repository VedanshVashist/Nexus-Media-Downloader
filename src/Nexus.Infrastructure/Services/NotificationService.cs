using Nexus.Core.Interfaces;

namespace Nexus.Infrastructure.Services;

/// <summary>
/// Default <see cref="INotificationService"/>: a thin publisher that raises an
/// event for each notification. The UI subscribes and renders toasts; nothing
/// here touches WPF, keeping it testable and reusable.
/// </summary>
public sealed class NotificationService : INotificationService
{
    public event EventHandler<Notification>? NotificationPublished;

    public void Notify(Notification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        NotificationPublished?.Invoke(this, notification);
    }

    public void Info(string message, string? title = null) =>
        Notify(new Notification { Message = message, Title = title, Level = NotificationLevel.Info });

    public void Success(string message, string? title = null) =>
        Notify(new Notification { Message = message, Title = title, Level = NotificationLevel.Success });

    public void Warning(string message, string? title = null) =>
        Notify(new Notification { Message = message, Title = title, Level = NotificationLevel.Warning });

    public void Error(string message, string? title = null) =>
        Notify(new Notification { Message = message, Title = title, Level = NotificationLevel.Error });
}
