using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexus.Core.DTOs;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using Nexus.Core.Exceptions;
using Nexus.Infrastructure.Dependencies;
using Nexus.Infrastructure.Processes;
using Nexus.Infrastructure.Settings;

namespace Nexus.Infrastructure.YtDlp;

/// <summary>
/// <see cref="IYtDlpService"/> implementation driving the yt-dlp executable via
/// <see cref="ProcessRunner"/>. Metadata comes from structured JSON
/// (<c>--dump-single-json</c>) mapped to the app's models; downloads stream a
/// custom progress template parsed into <see cref="DownloadProgress"/>.
/// </summary>
public sealed class YtDlpService : IYtDlpService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IDependencyManager _dependencies;
    private readonly ProcessRunner _processRunner;
    private readonly ILogger<YtDlpService> _logger;

    public YtDlpService(
        IDependencyManager dependencies,
        ProcessRunner processRunner,
        ILogger<YtDlpService> logger)
    {
        _dependencies = dependencies;
        _processRunner = processRunner;
        _logger = logger;
    }

    public async Task<string?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var exe = await ResolveExecutableAsync(cancellationToken).ConfigureAwait(false);
        var result = await _processRunner.RunAsync(exe, YtDlpArgumentBuilder.BuildVersionArguments(), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result.Success ? result.StandardOutput.Trim() : null;
    }

    public async Task<UrlAnalysisResult> AnalyzeAsync(string url, CancellationToken cancellationToken = default)
    {
        var root = await RunJsonAsync(YtDlpArgumentBuilder.BuildAnalyzeArguments(url), url, cancellationToken)
            .ConfigureAwait(false);

        if (YtDlpMapper.IsPlaylist(root))
        {
            _logger.LogInformation("Analyzed playlist with {Count} entries", root.Entries?.Count ?? 0);
            return UrlAnalysisResult.ForPlaylist(YtDlpMapper.ToPlaylistInfo(root));
        }

        return UrlAnalysisResult.ForVideo(YtDlpMapper.ToVideoInfo(root));
    }

    public async Task<VideoInfo> GetVideoInfoAsync(string url, CancellationToken cancellationToken = default)
    {
        var root = await RunJsonAsync(YtDlpArgumentBuilder.BuildInfoArguments(url), url, cancellationToken)
            .ConfigureAwait(false);
        return YtDlpMapper.ToVideoInfo(root);
    }

    public async Task<string> DownloadAsync(
        DownloadTask task,
        AppSettings settings,
        IProgress<DownloadProgress> progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(progress);

        var exe = await ResolveExecutableAsync(cancellationToken).ConfigureAwait(false);
        var outputDir = ResolveOutputDirectory(task, settings);
        Directory.CreateDirectory(outputDir);

        var ffmpegDir = await ResolveFfmpegDirectoryAsync(cancellationToken).ConfigureAwait(false);

        var args = YtDlpArgumentBuilder.BuildDownloadArguments(task, settings, outputDir, ffmpegDir);
        task.FormatSelector = string.Join(' ', args);

        string? finalPath = null;

        void OnLine(string line)
        {
            var parsed = YtDlpProgressParser.TryParse(line, task.Id);
            if (parsed is not null)
            {
                progress.Report(parsed);
                return;
            }

            // yt-dlp prints the final absolute path with --print after_move; as a
            // fallback we capture the "[download] Destination:" and merge lines.
            var dest = TryExtractDestination(line);
            if (dest is not null)
            {
                finalPath = dest;
            }
        }

        _logger.LogInformation("Starting yt-dlp download for task {TaskId}", task.Id);

        var result = await _processRunner.StreamAsync(
            exe,
            args,
            OnLine,
            onErrorLine: null,
            workingDirectory: outputDir,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            var tail = Tail(result.StandardError, 500);
            _logger.LogError("yt-dlp exited with code {Code} for task {TaskId}", result.ExitCode, task.Id);
            throw new YtDlpException(
                $"yt-dlp exited with code {result.ExitCode}.",
                userMessage: "Unable to download the selected format.",
                exitCode: result.ExitCode,
                standardError: tail);
        }

        return finalPath ?? outputDir;
    }

    private async Task<YtDlpRoot> RunJsonAsync(IReadOnlyList<string> args, string url, CancellationToken cancellationToken)
    {
        var exe = await ResolveExecutableAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Analyzing URL via yt-dlp");
        var result = await _processRunner.RunAsync(exe, args, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            var tail = Tail(result.StandardError, 500);
            throw new YtDlpException(
                $"yt-dlp analysis failed with code {result.ExitCode}.",
                userMessage: "Could not read information for that link.",
                exitCode: result.ExitCode,
                standardError: tail);
        }

        try
        {
            var root = JsonSerializer.Deserialize<YtDlpRoot>(result.StandardOutput, JsonOptions);
            if (root is null)
            {
                throw new YtDlpException("yt-dlp returned empty JSON.", userMessage: "No information was returned for that link.");
            }

            return root;
        }
        catch (JsonException ex)
        {
            throw new YtDlpException("Failed to parse yt-dlp JSON output.", userMessage: "The response from yt-dlp could not be understood.", innerException: ex);
        }
    }

    private async Task<string> ResolveExecutableAsync(CancellationToken cancellationToken)
    {
        var path = await _dependencies.ResolvePathAsync("yt-dlp", cancellationToken).ConfigureAwait(false);
        if (path is null)
        {
            throw new DependencyException("yt-dlp", "yt-dlp executable was not found.",
                userMessage: "yt-dlp is not available. Set its path in Settings or install it.");
        }

        return path;
    }

    private async Task<string?> ResolveFfmpegDirectoryAsync(CancellationToken cancellationToken)
    {
        var ffmpegPath = await _dependencies.ResolvePathAsync("ffmpeg", cancellationToken).ConfigureAwait(false);
        return ffmpegPath is null ? null : Path.GetDirectoryName(ffmpegPath);
    }

    private static string ResolveOutputDirectory(DownloadTask task, AppSettings settings)
    {
        var baseDir = task.Options.OutputDirectory
            ?? settings.Downloads.DefaultDownloadDirectory
            ?? AppPaths.DefaultDownloadDirectory();

        // Type-specific overrides.
        if (task.DownloadType == DownloadType.Audio && !string.IsNullOrWhiteSpace(settings.Downloads.AudioDirectory))
        {
            baseDir = settings.Downloads.AudioDirectory!;
        }
        else if (task.DownloadType == DownloadType.Video && !string.IsNullOrWhiteSpace(settings.Downloads.VideoDirectory))
        {
            baseDir = settings.Downloads.VideoDirectory!;
        }

        return baseDir;
    }

    private static string? TryExtractDestination(string line)
    {
        const string destMarker = "Destination:";
        var idx = line.IndexOf(destMarker, StringComparison.Ordinal);
        if (idx >= 0)
        {
            return line[(idx + destMarker.Length)..].Trim();
        }

        // "[Merger] Merging formats into \"C:\path\file.mp4\""
        const string mergeMarker = "Merging formats into";
        idx = line.IndexOf(mergeMarker, StringComparison.Ordinal);
        if (idx >= 0)
        {
            return line[(idx + mergeMarker.Length)..].Trim().Trim('"');
        }

        return null;
    }

    private static string Tail(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
        {
            return text;
        }

        return text[^maxChars..];
    }
}
