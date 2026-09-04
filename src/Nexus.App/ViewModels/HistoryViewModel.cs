using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nexus.App.Services;
using Nexus.Core.Interfaces;

namespace Nexus.App.ViewModels;

/// <summary>
/// The History page: lists past downloads newest-first with search, re-download,
/// favorite, and delete actions. Data is loaded lazily on first activation and can
/// be refreshed or cleared.
/// </summary>
public sealed partial class HistoryViewModel : PageViewModel
{
    private readonly IHistoryRepository _history;
    private readonly IFavoritesRepository _favorites;
    private readonly IDownloadManager _manager;
    private readonly ISystemAccess _system;
    private readonly INotificationService _notifications;
    private readonly IDialogService _dialogs;
    private readonly ILogger<HistoryViewModel> _logger;

    public HistoryViewModel(
        IHistoryRepository history,
        IFavoritesRepository favorites,
        IDownloadManager manager,
        ISystemAccess system,
        INotificationService notifications,
        IDialogService dialogs,
        ILogger<HistoryViewModel> logger)
        : base("history", "History", NavGlyph.History)
    {
        _history = history;
        _favorites = favorites;
        _manager = manager;
        _system = system;
        _notifications = notifications;
        _dialogs = dialogs;
        _logger = logger;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;
    }

    public ObservableCollection<HistoryItemViewModel> Items { get; } = [];

    public ICollectionView ItemsView { get; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _hasItems;

    public bool IsEmpty => !HasItems && !IsBusy;

    partial void OnSearchTextChanged(string value) => ItemsView.Refresh();

    protected override async Task OnFirstActivatedAsync() => await LoadAsync().ConfigureAwait(true);

    private bool FilterItem(object obj)
    {
        if (obj is not HistoryItemViewModel item)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var needle = SearchText.Trim();
        return item.Entry.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || item.Entry.Url.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || (item.Entry.Channel?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync().ConfigureAwait(true);

    private async Task LoadAsync()
    {
        IsBusy = true;
        OnPropertyChanged(nameof(IsEmpty));
        try
        {
            var entries = await _history.GetAllAsync().ConfigureAwait(true);

            Items.Clear();
            foreach (var entry in entries.OrderByDescending(e => e.DownloadedAt))
            {
                Items.Add(new HistoryItemViewModel(
                    entry, _manager, _favorites, _history, _system, _notifications, RemoveItem));
            }

            HasItems = Items.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load history.");
            _notifications.Error("Couldn't load history.", "History");
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private bool CanClear() => HasItems;

    [RelayCommand(CanExecute = nameof(CanClear))]
    private async Task ClearAsync()
    {
        if (!_dialogs.Confirm("Clear all download history? This cannot be undone.", "History"))
        {
            return;
        }

        await _history.ClearAsync().ConfigureAwait(true);
        Items.Clear();
        HasItems = false;
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void RemoveItem(HistoryItemViewModel item)
    {
        Items.Remove(item);
        HasItems = Items.Count > 0;
        OnPropertyChanged(nameof(IsEmpty));
    }
}
