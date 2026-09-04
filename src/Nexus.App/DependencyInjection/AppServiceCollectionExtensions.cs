using Microsoft.Extensions.DependencyInjection;
using Nexus.App.Services;
using Nexus.App.ViewModels;
using Nexus.Core.Interfaces;

namespace Nexus.App.DependencyInjection;

/// <summary>
/// Registers the WPF-layer services and view-models. Infrastructure services are
/// added separately via <c>AddNexusInfrastructure</c>; this method layers the UI
/// concerns (dispatcher, theming, dialogs, OS access, wallpapers) and the
/// view-model graph on top.
/// </summary>
public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddNexusApp(this IServiceCollection services)
    {
        // UI-thread marshalling used by every collection-bound view-model.
        services.AddSingleton<IUiDispatcher, UiDispatcher>();

        // UI-layer services (implementations live in Nexus.App because they touch WPF).
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ISystemAccess, SystemAccess>();
        services.AddSingleton<IWallpaperService, WallpaperService>();

        // Persists history when downloads finish; started during app bootstrap.
        services.AddSingleton<HistoryRecorder>();

        // Page view-models are singletons: they hold live subscriptions/state and
        // are composed once into the shell for the app's lifetime.
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<DownloadsViewModel>();
        services.AddSingleton<QueueViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<FavoritesViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AboutViewModel>();
        services.AddSingleton<MainViewModel>();

        // The first-run wizard is only shown once; a fresh instance is fine.
        services.AddTransient<FirstRunViewModel>();

        return services;
    }
}
