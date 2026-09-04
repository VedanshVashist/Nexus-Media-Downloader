using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nexus.App.Services;
using Nexus.Core.Constants;
using Nexus.Core.DTOs;
using Nexus.Core.Interfaces;

namespace Nexus.App.ViewModels;

/// <summary>
/// The About page: product info, resolved dependency versions, external links, and
/// a manual update check.
/// </summary>
public sealed partial class AboutViewModel : PageViewModel
{
    private readonly IUpdateService _updates;
    private readonly IDependencyManager _dependencies;
    private readonly ISystemAccess _system;
    private readonly INotificationService _notifications;
    private readonly ILogger<AboutViewModel> _logger;

    public AboutViewModel(
        IUpdateService updates,
        IDependencyManager dependencies,
        ISystemAccess system,
        INotificationService notifications,
        ILogger<AboutViewModel> logger)
        : base("about", "About", NavGlyph.About)
    {
        _updates = updates;
        _dependencies = dependencies;
        _system = system;
        _notifications = notifications;
        _logger = logger;

        IsPrimaryNavigation = false;
        AppVersion = ResolveVersion();
    }

    public string AppName => AppConstants.AppName;

    public string AppVersion { get; }

    public string GitHubUrl => AppLinks.GitHub;
    public string DocumentationUrl => AppLinks.Documentation;
    public string ReportIssueUrl => AppLinks.ReportIssue;

    public ObservableCollection<DependencyStatus> Dependencies { get; } = [];

    [ObservableProperty]
    private string? _updateStatus;

    protected override async Task OnFirstActivatedAsync()
    {
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
            _logger.LogWarning(ex, "Failed to read dependency versions for About page.");
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        UpdateStatus = "Checking for updates…";
        try
        {
            var info = await _updates.CheckForUpdatesAsync().ConfigureAwait(true);
            UpdateStatus = info.IsUpdateAvailable
                ? $"Version {info.LatestVersion} is available (you have {info.CurrentVersion})."
                : "You're up to date.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed.");
            UpdateStatus = "Couldn't check for updates.";
        }
    }

    [RelayCommand]
    private void OpenGitHub() => _system.OpenUrl(GitHubUrl);

    [RelayCommand]
    private void OpenDocumentation() => _system.OpenUrl(DocumentationUrl);

    [RelayCommand]
    private void ReportIssue() => _system.OpenUrl(ReportIssueUrl);

    private static string ResolveVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            // Strip build metadata (e.g. "1.0.0+abcdef") for display.
            var plus = info.IndexOf('+');
            return plus > 0 ? info[..plus] : info;
        }

        return assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    }
}
