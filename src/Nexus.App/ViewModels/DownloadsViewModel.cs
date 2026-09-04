using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nexus.App.Services;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.App.ViewModels;

/// <summary>
/// The Downloads page: a live view over every task the manager is tracking, with
/// search, status filtering, and bulk actions. Task cards project directly from
/// <see cref="IDownloadManager.Tasks"/> and dispose cleanly as tasks are removed.
/// </summary>
public sealed partial class DownloadsViewModel : PageViewModel, IDisposable
{
    private readonly IDownloadManager _manager;
    private readonly IUiDispatcher _ui;
    private readonly ObservableProjection<DownloadTask, DownloadItemViewModel> _projection;

    public DownloadsViewModel(
        IDownloadManager manager,
        IQueueService queue,
        ISystemAccess system,
        IUiDispatcher ui)
        : base("downloads", "Downloads", NavGlyph.Downloads)
    {
        _manager = manager;
        _ui = ui;

        _projection = new ObservableProjection<DownloadTask, DownloadItemViewModel>(
            manager.Tasks,
            task => new DownloadItemViewModel(task, manager, queue, system, ui),
            removed => removed.Dispose());

        ItemsView = CollectionViewSource.GetDefaultView(_projection.Items);
        ItemsView.Filter = FilterItem;

        ((INotifyCollectionChanged)_projection.Items).CollectionChanged += (_, _) => RecomputeCounts();
        _manager.TaskStatusChanged += OnTaskStatusChanged;
        RecomputeCounts();
    }

    /// <summary>Filtered/sorted view the page binds its list to.</summary>
    public ICollectionView ItemsView { get; }

    public IReadOnlyList<LabeledValue<DownloadFilter>> FilterChoices { get; } =
    [
        new(DownloadFilter.All, "All"),
        new(DownloadFilter.Active, "Active"),
        new(DownloadFilter.Completed, "Completed"),
        new(DownloadFilter.Failed, "Failed / cancelled")
    ];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DownloadFilter _statusFilter = DownloadFilter.All;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _hasItems;

    public bool IsEmpty => !HasItems;

    partial void OnSearchTextChanged(string value) => ItemsView.Refresh();

    partial void OnStatusFilterChanged(DownloadFilter value) => ItemsView.Refresh();

    private bool FilterItem(object obj)
    {
        if (obj is not DownloadItemViewModel item)
        {
            return false;
        }

        if (!StatusFilter.Matches(item.Model.Status))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var needle = SearchText.Trim();
        return item.Model.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || item.Model.Url.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private void OnTaskStatusChanged(object? sender, DownloadTask e) => _ui.Post(RecomputeCounts);

    private void RecomputeCounts()
    {
        var tasks = _manager.Tasks;
        TotalCount = tasks.Count;
        HasItems = tasks.Count > 0;
        ActiveCount = tasks.Count(t => DownloadFilter.Active.Matches(t.Status));
        CompletedCount = tasks.Count(t => t.Status == DownloadStatus.Completed);
    }

    [RelayCommand]
    private void PauseAll()
    {
        foreach (var task in Snapshot())
        {
            if (task.Status == DownloadStatus.Downloading)
            {
                _manager.TryPause(task.Id);
            }
        }
    }

    [RelayCommand]
    private async Task ResumeAllAsync()
    {
        foreach (var task in Snapshot())
        {
            if (task.Status == DownloadStatus.Paused)
            {
                await _manager.TryResumeAsync(task.Id).ConfigureAwait(true);
            }
        }
    }

    [RelayCommand]
    private void CancelAll()
    {
        foreach (var task in Snapshot())
        {
            if (task.CanCancel)
            {
                _manager.Cancel(task.Id);
            }
        }
    }

    [RelayCommand]
    private async Task RetryFailedAsync()
    {
        foreach (var task in Snapshot())
        {
            if (task.CanRetry)
            {
                await _manager.RetryAsync(task.Id).ConfigureAwait(true);
            }
        }
    }

    [RelayCommand]
    private void ClearFinished()
    {
        foreach (var task in Snapshot())
        {
            if (task.Status is DownloadStatus.Completed or DownloadStatus.Cancelled or DownloadStatus.Failed)
            {
                _manager.Remove(task.Id);
            }
        }
    }

    /// <summary>Copies the current tasks so bulk operations don't mutate during iteration.</summary>
    private List<DownloadTask> Snapshot() => _manager.Tasks.ToList();

    public void Dispose()
    {
        _manager.TaskStatusChanged -= OnTaskStatusChanged;
        _projection.Dispose();
    }
}
