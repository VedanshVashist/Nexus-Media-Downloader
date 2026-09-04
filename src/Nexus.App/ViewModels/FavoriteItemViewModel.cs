using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nexus.App.Services;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.App.ViewModels;

/// <summary>
/// A single favorite row: a saved URL the user can re-download, queue, open in the
/// browser, or remove. Favorites persist independently of history.
/// </summary>
public sealed partial class FavoriteItemViewModel : ObservableObject
{
    private readonly IDownloadManager _manager;
    private readonly IQueueService _queue;
    private readonly IFavoritesRepository _favorites;
    private readonly ISystemAccess _system;
    private readonly INotificationService _notifications;
    private readonly Action<FavoriteItemViewModel> _onDeleted;

    public FavoriteItemViewModel(
        FavoriteEntry entry,
        IDownloadManager manager,
        IQueueService queue,
        IFavoritesRepository favorites,
        ISystemAccess system,
        INotificationService notifications,
        Action<FavoriteItemViewModel> onDeleted)
    {
        Entry = entry;
        _manager = manager;
        _queue = queue;
        _favorites = favorites;
        _system = system;
        _notifications = notifications;
        _onDeleted = onDeleted;
    }

    public FavoriteEntry Entry { get; }

    private DownloadTask BuildTask() =>
        DownloadTaskFactory.Create(
            Entry.Url,
            new DownloadOptions { DownloadType = DownloadType.Video },
            Entry.Title,
            Entry.ThumbnailUrl);

    [RelayCommand]
    private async Task DownloadAsync()
    {
        await _manager.EnqueueAsync(BuildTask()).ConfigureAwait(true);
        _notifications.Success("Download started.", "Nexus");
    }

    [RelayCommand]
    private void AddToQueue()
    {
        _queue.Add(BuildTask());
        _notifications.Info("Added to queue.", "Queue");
    }

    [RelayCommand]
    private void OpenInBrowser() => _system.OpenUrl(Entry.Url);

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
    private async Task RemoveAsync()
    {
        await _favorites.DeleteAsync(Entry.Id).ConfigureAwait(true);
        _onDeleted(this);
    }
}
