using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Nexus.Core.Interfaces;
using Nexus.Core.Utilities;

namespace Nexus.App.Services;

/// <summary>
/// Launches OS handlers for files, folders, and URLs. Everything goes through
/// <see cref="ProcessStartInfo"/> with explicit argument lists — never a shell —
/// and all inputs are validated (existence / URL shape) before launching.
/// </summary>
public interface ISystemAccess
{
    void OpenFile(string? path);
    void RevealInExplorer(string? path);
    void OpenFolder(string? path);
    void OpenUrl(string? url);
}

/// <inheritdoc />
public sealed class SystemAccess : ISystemAccess
{
    private readonly INotificationService _notifications;
    private readonly ILogger<SystemAccess> _logger;

    public SystemAccess(INotificationService notifications, ILogger<SystemAccess> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    public void OpenFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _notifications.Warning("The file could not be found. It may have been moved or deleted.");
            return;
        }

        // UseShellExecute launches the registered handler via ShellExecute (not a
        // command shell), so no user text is ever parsed by cmd.exe/PowerShell.
        Launch(new ProcessStartInfo { FileName = path, UseShellExecute = true }, "open the file");
    }

    public void RevealInExplorer(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (File.Exists(path))
        {
            var info = new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = false };
            info.ArgumentList.Add("/select,");
            info.ArgumentList.Add(path);
            Launch(info, "open the folder");
        }
        else
        {
            OpenFolder(Path.GetDirectoryName(path));
        }
    }

    public void OpenFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            _notifications.Warning("That folder no longer exists.");
            return;
        }

        Launch(new ProcessStartInfo { FileName = path, UseShellExecute = true }, "open the folder");
    }

    public void OpenUrl(string? url)
    {
        if (!UrlValidator.IsValid(url, out var normalized) || normalized is null)
        {
            _notifications.Warning("That link is not a valid web address.");
            return;
        }

        Launch(new ProcessStartInfo { FileName = normalized, UseShellExecute = true }, "open the link");
    }

    private void Launch(ProcessStartInfo info, string action)
    {
        try
        {
            using var process = Process.Start(info);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to {Action}.", action);
            _notifications.Error($"Couldn't {action}.");
        }
    }
}
