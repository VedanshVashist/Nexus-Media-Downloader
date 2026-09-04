using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nexus.App.Services;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.App.ViewModels;

/// <summary>
/// The Queue page: an ordered staging list of tasks not yet handed to the manager.
/// Users reorder items, start them individually or all at once, and clear the queue.
/// Starting a task enqueues it with the manager and removes it from the queue.
/// </summary>
public sealed partial class QueueViewModel : PageViewModel, IDisposable
{
    private readonly IQueueService _queue;
    private readonly IDownloadManager _manager;
    private readonly ObservableProjection<DownloadTask, DownloadItemViewModel> _projection;

    public QueueViewModel(
        IQueueService queue,
        IDownloadManager manager,
        ISystemAccess system,
        IUiDispatcher ui)
        : base("queue", "Queue", NavGlyph.Queue)
    {
        _queue = queue;
        _manager = manager;

        _projection = new ObservableProjection<DownloadTask, DownloadItemViewModel>(
            queue.Items,
            task => new DownloadItemViewModel(task, manager, queue, system, ui),
            removed => removed.Dispose());

        ((INotifyCollectionChanged)Items).CollectionChanged += (_, _) => RecomputeCounts();
        RecomputeCounts();
    }

    public System.Collections.ObjectModel.ReadOnlyObservableCollection<DownloadItemViewModel> Items =>
        _projection.Items;

    [ObservableProperty]
    private DownloadItemViewModel? _selectedItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyCanExecuteChangedFor(nameof(StartAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearCommand))]
    private int _count;

    public bool IsEmpty => Count == 0;

    private void RecomputeCounts() => Count = _queue.Items.Count;

    private bool HasQueue() => _queue.Items.Count > 0;

    [RelayCommand(CanExecute = nameof(HasQueue))]
    private async Task StartAllAsync()
    {
        foreach (var task in _queue.Items.ToList())
        {
            _queue.Remove(task.Id);
            await _manager.EnqueueAsync(task).ConfigureAwait(true);
        }
    }

    [RelayCommand(CanExecute = nameof(HasQueue))]
    private void Clear() => _queue.Clear();

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedItem is not null)
        {
            _queue.Remove(SelectedItem.Model.Id);
        }
    }

    public void Dispose() => _projection.Dispose();
}
