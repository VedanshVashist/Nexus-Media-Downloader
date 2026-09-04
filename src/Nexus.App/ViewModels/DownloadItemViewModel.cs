using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nexus.App.Services;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.App.ViewModels;

/// <summary>
/// Wraps a single <see cref="DownloadTask"/> for display in the Downloads and Queue
/// lists. Exposes the row commands (pause/resume/cancel/retry/remove, open file/folder,
/// and queue reordering) and keeps their enabled state in sync with the task's status.
/// </summary>
public sealed partial class DownloadItemViewModel : ObservableObject, IDisposable
{
    private readonly IDownloadManager _manager;
    private readonly IQueueService _queue;
    private readonly ISystemAccess _system;
    private readonly IUiDispatcher _ui;
    private bool _disposed;

    public DownloadItemViewModel(
        DownloadTask model,
        IDownloadManager manager,
        IQueueService queue,
        ISystemAccess system,
        IUiDispatcher ui)
    {
        Model = model;
        _manager = manager;
        _queue = queue;
        _system = system;
        _ui = ui;

        Model.PropertyChanged += OnModelPropertyChanged;
    }

    /// <summary>The underlying task; the UI binds progress/title/status directly to it.</summary>
    public DownloadTask Model { get; }

    private void OnModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DownloadTask.Status) or nameof(DownloadTask.OutputPath))
        {
            // Status transitions arrive on the engine's background thread; refresh
            // command enabled-state on the UI thread where WPF requires it.
            _ui.Post(RefreshCommandStates);
        }
    }

    private void RefreshCommandStates()
    {
        PauseCommand.NotifyCanExecuteChanged();
        ResumeCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        RetryCommand.NotifyCanExecuteChanged();
        OpenFileCommand.NotifyCanExecuteChanged();
        OpenFolderCommand.NotifyCanExecuteChanged();
    }

    private bool CanPause() => Model.Status == DownloadStatus.Downloading;

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause() => _manager.TryPause(Model.Id);

    private bool CanResume() => Model.Status == DownloadStatus.Paused;

    [RelayCommand(CanExecute = nameof(CanResume))]
    private async Task ResumeAsync() => await _manager.TryResumeAsync(Model.Id).ConfigureAwait(true);

    private bool CanCancel() => Model.CanCancel;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _manager.Cancel(Model.Id);

    private bool CanRetry() => Model.CanRetry;

    [RelayCommand(CanExecute = nameof(CanRetry))]
    private async Task RetryAsync() => await _manager.RetryAsync(Model.Id).ConfigureAwait(true);

    [RelayCommand]
    private void Remove() => _manager.Remove(Model.Id);

    private bool CanOpenFile() =>
        Model.Status == DownloadStatus.Completed && !string.IsNullOrWhiteSpace(Model.OutputPath);

    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private void OpenFile() => _system.OpenFile(Model.OutputPath);

    private bool CanOpenFolder() => !string.IsNullOrWhiteSpace(Model.OutputPath);

    [RelayCommand(CanExecute = nameof(CanOpenFolder))]
    private void OpenFolder() => _system.RevealInExplorer(Model.OutputPath);

    [RelayCommand]
    private void CopyUrl()
    {
        try
        {
            Clipboard.SetText(Model.Url);
        }
        catch (Exception)
        {
            // The clipboard can be locked by another process; ignore transient failures.
        }
    }

    // --- Queue-only commands (used on the Queue page) ---

    [RelayCommand]
    private async Task StartAsync()
    {
        _queue.Remove(Model.Id);
        await _manager.EnqueueAsync(Model).ConfigureAwait(true);
    }

    [RelayCommand]
    private void MoveUp() => _queue.MoveUp(Model.Id);

    [RelayCommand]
    private void MoveDown() => _queue.MoveDown(Model.Id);

    [RelayCommand]
    private void RemoveFromQueue() => _queue.Remove(Model.Id);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Model.PropertyChanged -= OnModelPropertyChanged;
    }
}
