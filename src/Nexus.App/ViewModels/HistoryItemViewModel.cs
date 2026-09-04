using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nexus.App.Services;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.App.ViewModels;

/// <summary>
/// A single history row. Wraps a persisted <see cref="HistoryEntry"/> and exposes
/// the row actions: open the file/containing folder, re-download, toggle favorite,
/// copy the URL, and delete the entry (never the media file).
/// </summary>
public sealed partial class HistoryItemViewModel : ObservableObject
{
    private readonly IDownloadManager _manager;
    private readonly IFavoritesRepository _favorites;
    private readonly IHistoryRepository _history;
    private readonly ISystemAccess _system;
    private readonly INotificationService _notifications;
    private readonly Action<HistoryItemViewModel> _onDeleted;

    public HistoryItemViewModel(
        HistoryEntry entry,
        IDownloadManager manager,
        IFavoritesRepository favorites,
        IHistoryRepository history,
        ISystemAccess system,
        INotificationService notifications,
        Action<HistoryItemViewModel> onDeleted)
    {
        Entry = entry;
        _manager = manager;
        _favorites = favorites;
        _history = history;
        _system = system;
        _notifications = notifications;
        _onDeleted = onDeleted;
        _isFavorite = entry.IsFavorite;
    }

    public HistoryEntry Entry { get; }

    [ObservableProperty]
    private bool _isFavorite;

    private bool CanOpenFile() => !string.IsNullOrWhiteSpace(Entry.FilePath);

    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private void OpenFile() => _system.OpenFile(Entry.FilePath);

    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private void OpenFolder() => _system.RevealInExplorer(Entry.FilePath);

    [RelayCommand]
    private async Task RedownloadAsync()
    {
        var options = new DownloadOptions { DownloadType = Entry.DownloadType };
        var task = DownloadTaskFactory.Create(Entry.Url, options, Entry.Title, Entry.ThumbnailUrl);
        await _manager.EnqueueAsync(task).ConfigureAwait(true);
        _notifications.Success("Re-downloading.", "Nexus");
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (IsFavorite)
        {
            var all = await _favorites.GetAllAsync().ConfigureAwait(true);
            foreach (var fav in all.Where(f => string.Equals(f.Url, Entry.Url, StringComparison.OrdinalIgnoreCase)))
            {
                await _favorites.DeleteAsync(fav.Id).ConfigureAwait(true);
            }

            IsFavorite = false;
            _notifications.Info("Removed from favorites.", "Favorites");
        }
        else
        {
            await _favorites.AddAsync(new FavoriteEntry
            {
                Url = Entry.Url,
                Title = Entry.Title,
                Channel = Entry.Channel,
                ThumbnailUrl = Entry.ThumbnailUrl,
                ThumbnailPath = Entry.ThumbnailPath
            }).ConfigureAwait(true);

            IsFavorite = true;
            _notifications.Success("Saved to favorites.", "Favorites");
        }

        Entry.IsFavorite = IsFavorite;
    }

    [RelayCommand]
    private void CopyUrl()
    {
        try
        {
            Clipboard.SetText(Entry.Url);
        }
        catch (Exception)
        {
            // Clipboard may be locked; ignore.
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        await _history.DeleteAsync(Entry.Id).ConfigureAwait(true);
        _onDeleted(this);
    }
}
