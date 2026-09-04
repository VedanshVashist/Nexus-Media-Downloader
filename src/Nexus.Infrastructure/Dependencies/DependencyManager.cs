using System.Net.Http;
using Microsoft.Extensions.Logging;
using Nexus.Core.Constants;
using Nexus.Core.DTOs;
using Nexus.Core.Interfaces;
using Nexus.Infrastructure.Processes;
using Nexus.Infrastructure.Settings;

namespace Nexus.Infrastructure.Dependencies;

/// <summary>
/// Locates and validates yt-dlp, ffmpeg, and ffprobe. Discovery order per tool:
/// (1) explicit path in settings, (2) bundled tools folder next to the exe,
/// (3) the system PATH. Version probing runs the tool with a version flag.
/// Automatic updates pull only from official release URLs.
/// </summary>
public sealed class DependencyManager : IDependencyManager
{
    private const string YtDlp = "yt-dlp";
    private const string FFmpeg = "ffmpeg";
    private const string FFprobe = "ffprobe";

    private readonly ISettingsService _settings;
    private readonly AppPaths _paths;
    private readonly ProcessRunner _processRunner;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DependencyManager> _logger;

    public DependencyManager(
        ISettingsService settings,
        AppPaths paths,
        ProcessRunner processRunner,
        IHttpClientFactory httpClientFactory,
        ILogger<DependencyManager> logger)
    {
        _settings = settings;
        _paths = paths;
        _processRunner = processRunner;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DependencyStatus>> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(
            CheckAsync(YtDlp, cancellationToken),
            CheckAsync(FFmpeg, cancellationToken),
            CheckAsync(FFprobe, cancellationToken)).ConfigureAwait(false);

        return results;
    }

    public async Task<DependencyStatus> CheckAsync(string dependencyName, CancellationToken cancellationToken = default)
    {
        var path = await ResolvePathAsync(dependencyName, cancellationToken).ConfigureAwait(false);
        if (path is null)
        {
            return new DependencyStatus
            {
                Name = dependencyName,
                IsAvailable = false,
                Detail = $"{dependencyName} was not found in settings, the bundled tools folder, or PATH."
            };
        }

        var version = await TryGetVersionAsync(dependencyName, path, cancellationToken).ConfigureAwait(false);
        return new DependencyStatus
        {
            Name = dependencyName,
            IsAvailable = version is not null,
            Path = path,
            Version = version,
            Detail = version is null ? "Executable found but did not return a version." : null
        };
    }

    public Task<string?> ResolvePathAsync(string dependencyName, CancellationToken cancellationToken = default)
    {
        var configured = GetConfiguredPath(dependencyName);
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            return Task.FromResult<string?>(configured);
        }

        var exeName = ToExecutableName(dependencyName);

        // Bundled tools folder next to the executable.
        var bundled = Path.Combine(_paths.BundledToolsDirectory, exeName);
        if (File.Exists(bundled))
        {
            return Task.FromResult<string?>(bundled);
        }

        // System PATH.
        var fromPath = FindOnPath(exeName);
        return Task.FromResult(fromPath);
    }

    public async Task<string> UpdateYtDlpAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_paths.BundledToolsDirectory);
        var targetPath = Path.Combine(_paths.BundledToolsDirectory, "yt-dlp.exe");

        _logger.LogInformation("Updating yt-dlp from official release: {Url}", AppLinks.YtDlpWindowsBinary);

        var client = _httpClientFactory.CreateClient("downloads");
        using var response = await client.GetAsync(
            AppLinks.YtDlpWindowsBinary,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        var tempPath = targetPath + ".tmp";

        await using (var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        await using (var fileStream = File.Create(tempPath))
        {
            var buffer = new byte[81920];
            long readTotal = 0;
            int read;
            while ((read = await httpStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                readTotal += read;
                if (total is > 0)
                {
                    progress?.Report(Math.Clamp(readTotal / (double)total.Value * 100.0, 0, 100));
                }
            }
        }

        File.Move(tempPath, targetPath, overwrite: true);
        _logger.LogInformation("yt-dlp updated at {Path}", targetPath);
        return targetPath;
    }

    private string? GetConfiguredPath(string dependencyName) => dependencyName switch
    {
        YtDlp => _settings.Current.YtDlp.ExecutablePath,
        FFmpeg => _settings.Current.FFmpeg.ExecutablePath,
        FFprobe => _settings.Current.FFmpeg.FFprobePath,
        _ => null
    };

    private static string ToExecutableName(string dependencyName)
    {
        var isWindows = OperatingSystem.IsWindows();
        return dependencyName switch
        {
            YtDlp => isWindows ? "yt-dlp.exe" : "yt-dlp",
            FFmpeg => isWindows ? "ffmpeg.exe" : "ffmpeg",
            FFprobe => isWindows ? "ffprobe.exe" : "ffprobe",
            _ => dependencyName
        };
    }

    private static string? FindOnPath(string exeName)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
        {
            return null;
        }

        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), exeName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // Ignore malformed PATH entries.
            }
        }

        return null;
    }

    private async Task<string?> TryGetVersionAsync(string dependencyName, string path, CancellationToken cancellationToken)
    {
        try
        {
            // yt-dlp uses --version; ffmpeg/ffprobe use -version.
            var versionArg = dependencyName == YtDlp ? "--version" : "-version";
            var result = await _processRunner.RunAsync(path, [versionArg], cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!result.Success)
            {
                return null;
            }

            var output = string.IsNullOrWhiteSpace(result.StandardOutput)
                ? result.StandardError
                : result.StandardOutput;

            return ExtractVersion(dependencyName, output);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to probe version for {Dependency}", dependencyName);
            return null;
        }
    }

    private static string? ExtractVersion(string dependencyName, string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (firstLine is null)
        {
            return null;
        }

        if (dependencyName == YtDlp)
        {
            return firstLine;
        }

        // "ffmpeg version 6.1.1 Copyright ..." -> "6.1.1"
        var tokens = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var idx = Array.FindIndex(tokens, t => t.Equals("version", StringComparison.OrdinalIgnoreCase));
        if (idx >= 0 && idx + 1 < tokens.Length)
        {
            return tokens[idx + 1];
        }

        return firstLine;
    }
}
