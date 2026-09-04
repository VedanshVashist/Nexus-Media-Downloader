using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nexus.Core.Interfaces;

namespace Nexus.App.ViewModels;

/// <summary>
/// A single on-screen toast. Wraps a <see cref="Notification"/> and auto-dismisses
/// after its duration; the user can also close it early. Created on the UI thread
/// so its <see cref="DispatcherTimer"/> ticks there.
/// </summary>
public sealed partial class ToastViewModel : ObservableObject
{
    private readonly DispatcherTimer _timer;
    private readonly Action<ToastViewModel> _onDismiss;

    public ToastViewModel(Notification notification, Action<ToastViewModel> onDismiss)
    {
        Notification = notification;
        _onDismiss = onDismiss;

        _timer = new DispatcherTimer { Interval = notification.Duration };
        _timer.Tick += (_, _) => Dismiss();
        _timer.Start();
    }

    public Notification Notification { get; }

    public string Message => Notification.Message;
    public string? Title => Notification.Title;
    public bool HasTitle => !string.IsNullOrWhiteSpace(Notification.Title);
    public NotificationLevel Level => Notification.Level;

    [RelayCommand]
    private void Close() => Dismiss();

    private void Dismiss()
    {
        _timer.Stop();
        _onDismiss(this);
    }
}
