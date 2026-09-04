using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nexus.App.Services;
using Nexus.Core.Constants;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.App.ViewModels;

/// <summary>
/// The application shell view-model: owns the page list and current-page selection,
/// hosts the toast stack, and exposes wallpaper/appearance state for the window
/// chrome. Bridges notifications and Home's navigation requests to the UI.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private const int MaxVisibleToasts = 5;

    private readonly INotificationService _notifications;
    private readonly ISettingsService _settings;
    private readonly IWallpaperService _wallpapers;
    private readonly IDownloadManager _manager;
    private readonly IUiDispatcher _ui;
    private readonly HomeViewModel _home;

    public MainViewModel(
        HomeViewModel home,
        DownloadsViewModel downloads,
        QueueViewModel queue,
        HistoryViewModel history,
        FavoritesViewModel favorites,
        SettingsViewModel settings,
        AboutViewModel about,
        INotificationService notifications,
        ISettingsService settingsService,
        IWallpaperService wallpapers,
        IDownloadManager manager,
        IUiDispatcher ui)
    {
        _home = home;
        _notifications = notifications;
        _settings = settingsService;
        _wallpapers = wallpapers;
        _manager = manager;
        _ui = ui;

        PrimaryPages = [home, downloads, queue, history, favorites];
        SecondaryPages = [settings, about];
        AllPages = [.. PrimaryPages, .. SecondaryPages];

        _currentPage = home;

        _home.NavigateToDownloadsRequested += () => Navigate(downloads);
        _home.NavigateToQueueRequested += () => Navigate(queue);

        _notifications.NotificationPublished += OnNotificationPublished;
        _settings.SettingsChanged += OnSettingsChanged;
        _manager.TaskStatusChanged += OnTaskStatusChanged;
        ((INotifyCollectionChanged)_manager.Tasks).CollectionChanged += (_, _) => _ui.Post(RecomputeActive);

        ApplyAppearance(_settings.Current);
        RecomputeActive();

        // Activate the initial page.
        _ = CurrentPage.OnActivatedAsync();
    }

    public string Title => AppConstants.AppName;

    public IReadOnlyList<PageViewModel> PrimaryPages { get; }
    public IReadOnlyList<PageViewModel> SecondaryPages { get; }
    public IReadOnlyList<PageViewModel> AllPages { get; }

    public ObservableCollection<ToastViewModel> Toasts { get; } = [];

    [ObservableProperty]
    private PageViewModel _currentPage;

    [ObservableProperty]
    private int _activeDownloadCount;

    // --- Appearance / wallpaper (bound by the window chrome) ---
    [ObservableProperty] private string? _wallpaperPath;
    [ObservableProperty] private bool _wallpaperVisible;
    [ObservableProperty] private double _wallpaperOpacity;
    [ObservableProperty] private double _wallpaperBlur;
    [ObservableProperty] private double _wallpaperDarkness;
    [ObservableProperty] private string _wallpaperStretch = "UniformToFill";
    [ObservableProperty] private bool _transparencyEnabled = true;
    [ObservableProperty] private bool _animationEnabled = true;
    [ObservableProperty] private bool _compactMode;

    [RelayCommand]
    private void Navigate(PageViewModel? page)
    {
        if (page is null)
        {
            return;
        }

        // Setting the property activates the page via OnCurrentPageChanged. When the
        // page is already current, re-run activation so an explicit nav still refreshes.
        if (ReferenceEquals(page, CurrentPage))
        {
            _ = page.OnActivatedAsync();
            return;
        }

        CurrentPage = page;
    }

    // Activation is centralized here so navigation works identically whether it is
    // driven by NavigateCommand or by a two-way binding on the sidebar selection.
    partial void OnCurrentPageChanged(PageViewModel value) => _ = value.OnActivatedAsync();

    [RelayCommand]
    private void OpenSettings() => Navigate(SecondaryPages[0]);

    [RelayCommand]
    private void GoHome() => Navigate(_home);

    private void OnNotificationPublished(object? sender, Notification e) => _ui.Post(() => AddToast(e));

    private void AddToast(Notification notification)
    {
        Toasts.Insert(0, new ToastViewModel(notification, RemoveToast));
        while (Toasts.Count > MaxVisibleToasts)
        {
            Toasts.RemoveAt(Toasts.Count - 1);
        }
    }

    private void RemoveToast(ToastViewModel toast) => Toasts.Remove(toast);

    private void OnSettingsChanged(object? sender, AppSettings e) => _ui.Post(() => ApplyAppearance(e));

    private void ApplyAppearance(AppSettings settings)
    {
        var a = settings.Appearance;
        TransparencyEnabled = a.TransparencyEnabled;
        AnimationEnabled = a.AnimationEnabled;
        CompactMode = a.CompactMode;
        WallpaperOpacity = a.WallpaperOpacity;
        WallpaperBlur = a.TransparencyEnabled ? a.WallpaperBlur : 0;
        WallpaperDarkness = a.WallpaperDarkness;
        WallpaperStretch = string.IsNullOrWhiteSpace(a.WallpaperStretch) ? "UniformToFill" : a.WallpaperStretch;

        var path = a.WallpaperEnabled ? _wallpapers.ResolvePath(a.WallpaperFileName) : null;
        WallpaperPath = path;
        WallpaperVisible = a.WallpaperEnabled && path is not null;
    }

    private void OnTaskStatusChanged(object? sender, DownloadTask e) => _ui.Post(RecomputeActive);

    private void RecomputeActive() =>
        ActiveDownloadCount = _manager.Tasks.Count(t => t.Status is DownloadStatus.Downloading
            or DownloadStatus.Queued or DownloadStatus.Processing);

    public void Dispose()
    {
        _notifications.NotificationPublished -= OnNotificationPublished;
        _settings.SettingsChanged -= OnSettingsChanged;
        _manager.TaskStatusChanged -= OnTaskStatusChanged;

        (_home as IDisposable)?.Dispose();
        foreach (var page in AllPages.OfType<IDisposable>())
        {
            page.Dispose();
        }
    }
}
