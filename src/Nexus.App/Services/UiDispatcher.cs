using System.Windows;
using System.Windows.Threading;

namespace Nexus.App.Services;

/// <summary>
/// Abstraction over the WPF UI dispatcher so view-models can marshal work raised on
/// background threads (download-manager events, notifications) onto the UI thread
/// before touching bound collections.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>True when the caller is already on the UI thread.</summary>
    bool CheckAccess();

    /// <summary>Posts an action to run asynchronously on the UI thread.</summary>
    void Post(Action action);

    /// <summary>Runs an action on the UI thread, synchronously if already on it.</summary>
    void Invoke(Action action);

    /// <summary>Awaitable invoke on the UI thread.</summary>
    Task InvokeAsync(Action action);
}

/// <summary>Default <see cref="IUiDispatcher"/> backed by the application's dispatcher.</summary>
public sealed class UiDispatcher : IUiDispatcher
{
    private static Dispatcher Dispatcher =>
        Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

    public bool CheckAccess() => Dispatcher.CheckAccess();

    public void Post(Action action) => Dispatcher.BeginInvoke(action, DispatcherPriority.Background);

    public void Invoke(Action action)
    {
        if (Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.Invoke(action);
        }
    }

    public Task InvokeAsync(Action action) => Dispatcher.InvokeAsync(action).Task;
}
