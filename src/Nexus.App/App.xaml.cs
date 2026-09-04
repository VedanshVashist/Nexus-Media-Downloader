using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.App.DependencyInjection;
using Nexus.App.Services;
using Nexus.App.ViewModels;
using Nexus.App.Views;
using Nexus.Core.Interfaces;
using Nexus.Infrastructure.Database;
using Nexus.Infrastructure.DependencyInjection;
using Nexus.Infrastructure.Settings;
using Serilog;

namespace Nexus.App;

/// <summary>
/// Application entry point. Builds the Generic Host + DI container, configures
/// Serilog, initializes storage/settings/theme, then shows the first-run wizard or
/// the main shell. Owns global exception handling and orderly shutdown.
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private ILogger<App>? _logger;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handlers are wired first so failures during init are logged.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            await InitializeAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error during startup.");
            MessageBox.Show(
                "Nexus failed to start. Please check the log files for details.",
                "Nexus",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private async Task InitializeAsync()
    {
        // Resolve data locations and ensure the folder tree exists before logging.
        var paths = new AppPaths();
        paths.EnsureCreated();

        // Let the wallpaper thumbnail converter resolve stored file names to full paths.
        Converters.WallpaperImageConverter.Directory = paths.WallpapersDirectory;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Debug()
            .WriteTo.File(
                Path.Combine(paths.LogsDirectory, "nexus-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddNexusInfrastructure();
                services.AddNexusApp();

                // Reuse the AppPaths instance whose directories we already created.
                services.AddSingleton(paths);
            })
            .Build();

        await _host.StartAsync().ConfigureAwait(true);

        var services = _host.Services;
        _logger = services.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("Nexus starting up.");

        var settings = services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync().ConfigureAwait(true);

        await EnsureDatabaseAsync(services).ConfigureAwait(true);

        // Apply the saved theme + accent before any window is shown.
        var theme = services.GetRequiredService<IThemeService>();
        theme.ApplyTheme(settings.Current.Appearance.Theme);
        theme.ApplyAccentColor(settings.Current.Appearance.AccentColor);

        services.GetRequiredService<IDownloadManager>()
            .SetMaxConcurrency(settings.Current.Downloads.MaxConcurrentDownloads);

        // Begin recording completed downloads to history.
        services.GetRequiredService<HistoryRecorder>().Start();

        if (settings.Current.FirstRunCompleted)
        {
            ShowMainWindow();
        }
        else
        {
            ShowFirstRunWindow();
        }
    }

    private async Task EnsureDatabaseAsync(IServiceProvider services)
    {
        try
        {
            var factory = services.GetRequiredService<IDbContextFactory<NexusDbContext>>();
            await using var db = await factory.CreateDbContextAsync().ConfigureAwait(true);
            await db.Database.EnsureCreatedAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize the database.");
            throw;
        }
    }

    private void ShowFirstRunWindow()
    {
        var vm = _host!.Services.GetRequiredService<FirstRunViewModel>();
        var window = new FirstRunWindow { DataContext = vm };

        var completed = false;
        vm.Completed += () =>
        {
            completed = true;
            window.Close();
        };

        window.Closed += (_, _) =>
        {
            if (completed)
            {
                ShowMainWindow();
            }
            else
            {
                // The user closed the wizard without finishing: exit cleanly.
                Shutdown();
            }
        };

        window.Show();
    }

    private void ShowMainWindow()
    {
        var vm = _host!.Services.GetRequiredService<MainViewModel>();
        var window = new MainWindow { DataContext = vm };
        MainWindow = window;
        window.Closed += (_, _) => Shutdown();
        window.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unhandled UI exception.");
        TryNotify("Something went wrong. The action couldn't be completed.");

        // Keep the app alive for non-fatal UI exceptions.
        e.Handled = true;
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger?.LogCritical(ex, "Unhandled non-UI exception. Terminating: {Terminating}", e.IsTerminating);
            Log.Fatal(ex, "Unhandled non-UI exception.");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogWarning(e.Exception, "Unobserved task exception.");
        e.SetObserved();
    }

    private void TryNotify(string message)
    {
        try
        {
            _host?.Services.GetService<INotificationService>()?.Error(message);
        }
        catch
        {
            // Notifications are best-effort during failure handling.
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host is not null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                _host.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error during shutdown.");
        }
        finally
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
            base.OnExit(e);
        }
    }
}
