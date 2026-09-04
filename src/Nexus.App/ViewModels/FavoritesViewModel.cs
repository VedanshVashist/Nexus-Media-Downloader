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
/// The Favorites page: saved URLs for quick re-download, with search and per-row
/// actions. Loaded lazily on first activation and refreshed on demand.
/// </summary>
public sealed partial class FavoritesViewModel : PageViewModel
{
    private readonly IFavoritesRepository _favorites;
    private readonly IDownloadManager _manager;
    private readonly IQueueService _queue;
    private readonly ISystemAccess _system;
    private readonly INotificationService _notifications;
    private readonly ILogger<FavoritesViewModel> _logger;

    public FavoritesViewModel(
        IFavoritesRepository favorites,
        IDownloadManager manager,
        IQueueService queue,
        ISystemAccess system,
        INotificationService notifications,
        ILogger<FavoritesViewModel> logger)
        : base("favorites", "Favorites", NavGlyph.Favorites)
    {
        _favorites = favorites;
        _manager = manager;
        _queue = queue;
        _system = system;
        _notifications = notifications;
        _logger = logger;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;
    }

    public ObservableCollection<FavoriteItemViewModel> Items { get; } = [];

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
        if (obj is not FavoriteItemViewModel item)
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
            var entries = await _favorites.GetAllAsync().ConfigureAwait(true);

            Items.Clear();
            foreach (var entry in entries.OrderByDescending(e => e.AddedAt))
            {
                Items.Add(new FavoriteItemViewModel(
                    entry, _manager, _queue, _favorites, _system, _notifications, RemoveItem));
            }

            HasItems = Items.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load favorites.");
            _notifications.Error("Couldn't load favorites.", "Favorites");
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private void RemoveItem(FavoriteItemViewModel item)
    {
        Items.Remove(item);
        HasItems = Items.Count > 0;
        OnPropertyChanged(nameof(IsEmpty));
        _notifications.Info("Removed from favorites.", "Favorites");
    }
}
