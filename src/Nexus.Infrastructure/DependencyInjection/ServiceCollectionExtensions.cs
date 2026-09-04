using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Core.Interfaces;
using Nexus.Infrastructure.Database;
using Nexus.Infrastructure.Dependencies;
using Nexus.Infrastructure.Downloads;
using Nexus.Infrastructure.FFmpeg;
using Nexus.Infrastructure.Processes;
using Nexus.Infrastructure.Services;
using Nexus.Infrastructure.Settings;
using Nexus.Infrastructure.YtDlp;

namespace Nexus.Infrastructure.DependencyInjection;

/// <summary>
/// Registers all Infrastructure services with the DI container. Keeping this in
/// one place lets the WPF app compose the graph with a single call and keeps
/// wiring out of the UI layer.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNexusInfrastructure(this IServiceCollection services)
    {
        // Paths + process runner are shared singletons.
        services.AddSingleton<AppPaths>();
        services.AddSingleton<ProcessRunner>();

        // HttpClient factory for thumbnail/tool downloads with a sane user agent.
        services.AddHttpClient("downloads", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Nexus/1.0 (+https://github.com/your-org/nexus)");
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Settings must be a singleton so Current is shared app-wide.
        services.AddSingleton<ISettingsService, SettingsService>();

        // External tool integration.
        services.AddSingleton<IDependencyManager, DependencyManager>();
        services.AddSingleton<IYtDlpService, YtDlpService>();
        services.AddSingleton<IFFmpegService, FFmpegService>();

        // Downloads + queue.
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IDownloadManager, DownloadManager>();
        services.AddSingleton<IQueueService, QueueService>();

        // Support services.
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IThumbnailService, ThumbnailService>();
        services.AddSingleton<IUpdateService, UpdateService>();

        // Database: factory so repositories get short-lived contexts.
        services.AddDbContextFactory<NexusDbContext>((sp, options) =>
        {
            var paths = sp.GetRequiredService<AppPaths>();
            options.UseSqlite($"Data Source={paths.DatabasePath}");
        });

        services.AddSingleton<IHistoryRepository, HistoryRepository>();
        services.AddSingleton<IFavoritesRepository, FavoritesRepository>();

        return services;
    }
}
