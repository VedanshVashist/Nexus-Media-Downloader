using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nexus.App.Services;
using Nexus.Core.DTOs;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;
using Nexus.Infrastructure.Settings;

namespace Nexus.App.ViewModels;

/// <summary>Wizard steps shown on first launch.</summary>
public enum FirstRunStep
{
    Welcome = 0,
    Dependencies = 1,
    Preferences = 2,
    Finish = 3
}

/// <summary>
/// Drives the first-run wizard: welcome, dependency setup (optionally fetching
/// yt-dlp from its official source), basic preferences, and finish. On completion
/// it persists the chosen defaults and marks first-run done.
/// </summary>
public sealed partial class FirstRunViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _themeService;
    private readonly IDependencyManager _dependencies;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notifications;
    private readonly ILogger<FirstRunViewModel> _logger;

    public FirstRunViewModel(
        ISettingsService settings,
        IThemeService themeService,
        IDependencyManager dependencies,
        IDialogService dialogs,
        INotificationService notifications,
        ILogger<FirstRunViewModel> logger)
    {
        _settings = settings;
        _themeService = themeService;
        _dependencies = dependencies;
        _dialogs = dialogs;
        _notifications = notifications;
        _logger = logger;

        var appearance = settings.Current.Appearance;
        _selectedTheme = appearance.Theme;
        _defaultDirectory = settings.Current.Downloads.DefaultDownloadDirectory
            ?? AppPaths.DefaultDownloadDirectory();
    }

    /// <summary>Raised when the wizard finishes (or is skipped) and the shell should open.</summary>
    public event Action? Completed;

    public IReadOnlyList<LabeledValue<ThemeType>> ThemeChoices { get; } =
    [
        new(ThemeType.Midnight, "Midnight"),
        new(ThemeType.Aurora, "Aurora"),
        new(ThemeType.Crimson, "Crimson"),
        new(ThemeType.Cyberpunk, "Cyberpunk")
    ];

    public ObservableCollection<DependencyStatus> Dependencies { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWelcome))]
    [NotifyPropertyChangedFor(nameof(IsDependencies))]
    [NotifyPropertyChangedFor(nameof(IsPreferences))]
    [NotifyPropertyChangedFor(nameof(IsFinish))]
    [NotifyPropertyChangedFor(nameof(NextLabel))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    private FirstRunStep _step = FirstRunStep.Welcome;

    [ObservableProperty]
    private ThemeType _selectedTheme;

    [ObservableProperty]
    private string _defaultDirectory;

    [ObservableProperty]
    private bool _isCheckingDependencies;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadYtDlpCommand))]
    private bool _isDownloadingYtDlp;

    [ObservableProperty]
    private double _ytDlpProgress;

    public bool IsWelcome => Step == FirstRunStep.Welcome;
    public bool IsDependencies => Step == FirstRunStep.Dependencies;
    public bool IsPreferences => Step == FirstRunStep.Preferences;
    public bool IsFinish => Step == FirstRunStep.Finish;
    public string NextLabel => Step == FirstRunStep.Finish ? "Get started" : "Next";

    partial void OnSelectedThemeChanged(ThemeType value) => _themeService.ApplyTheme(value);

    [RelayCommand]
    private async Task NextAsync()
    {
        if (Step == FirstRunStep.Finish)
        {
            await FinishAsync().ConfigureAwait(true);
            return;
        }

        Step++;

        if (Step == FirstRunStep.Dependencies && Dependencies.Count == 0)
        {
            await CheckDependenciesAsync().ConfigureAwait(true);
        }
    }

    private bool CanGoBack() => Step > FirstRunStep.Welcome;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        if (Step > FirstRunStep.Welcome)
        {
            Step--;
        }
    }

    [RelayCommand]
    private void BrowseDirectory()
    {
        var chosen = _dialogs.PickFolder("Choose where downloads are saved", DefaultDirectory);
        if (chosen is not null)
        {
            DefaultDirectory = chosen;
        }
    }

    [RelayCommand]
    private async Task CheckDependenciesAsync()
    {
        IsCheckingDependencies = true;
        try
        {
            var statuses = await _dependencies.CheckAllAsync().ConfigureAwait(true);
            Dependencies.Clear();
            foreach (var status in statuses)
            {
                Dependencies.Add(status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "First-run dependency check failed.");
        }
        finally
        {
            IsCheckingDependencies = false;
        }
    }

    private bool CanDownloadYtDlp() => !IsDownloadingYtDlp;

    [RelayCommand(CanExecute = nameof(CanDownloadYtDlp))]
    private async Task DownloadYtDlpAsync()
    {
        IsDownloadingYtDlp = true;
        YtDlpProgress = 0;
        try
        {
            var progress = new Progress<double>(p => YtDlpProgress = p);
            await _dependencies.UpdateYtDlpAsync(progress).ConfigureAwait(true);
            _notifications.Success("yt-dlp is ready.", "Setup");
            await CheckDependenciesAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "First-run yt-dlp download failed.");
            _notifications.Error("Couldn't download yt-dlp. You can set its path later in Settings.", "Setup");
        }
        finally
        {
            IsDownloadingYtDlp = false;
        }
    }

    [RelayCommand]
    private async Task SkipAsync() => await FinishAsync().ConfigureAwait(true);

    private async Task FinishAsync()
    {
        try
        {
            var updated = _settings.Current.Clone();
            updated.Appearance.Theme = SelectedTheme;
            updated.Downloads.DefaultDownloadDirectory =
                string.IsNullOrWhiteSpace(DefaultDirectory) ? null : DefaultDirectory.Trim();
            updated.FirstRunCompleted = true;

            await _settings.SaveAsync(updated).ConfigureAwait(true);
            _themeService.ApplyTheme(SelectedTheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist first-run settings.");
        }
        finally
        {
            Completed?.Invoke();
        }
    }
}
