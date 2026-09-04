using System.Text;
using Microsoft.Extensions.Logging;
using Nexus.Core.Exceptions;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using Nexus.Infrastructure.Processes;

namespace Nexus.Infrastructure.FFmpeg;

/// <summary>
/// <see cref="IFFmpegService"/> implementation. yt-dlp handles most muxing during
/// downloads, so this service covers standalone post-processing and availability
/// detection. All invocations go through <see cref="ProcessRunner"/> with discrete
/// arguments — never a shell.
/// </summary>
public sealed class FFmpegService : IFFmpegService
{
    private readonly IDependencyManager _dependencies;
    private readonly ProcessRunner _processRunner;
    private readonly ILogger<FFmpegService> _logger;

    public FFmpegService(
        IDependencyManager dependencies,
        ProcessRunner processRunner,
        ILogger<FFmpegService> logger)
    {
        _dependencies = dependencies;
        _processRunner = processRunner;
        _logger = logger;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var path = await _dependencies.ResolvePathAsync("ffmpeg", cancellationToken).ConfigureAwait(false);
        return path is not null;
    }

    public async Task<string?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var status = await _dependencies.CheckAsync("ffmpeg", cancellationToken).ConfigureAwait(false);
        return status.Version;
    }

    public async Task MergeAsync(string videoPath, string audioPath, string outputPath, CancellationToken cancellationToken = default)
    {
        // Copy both streams without re-encoding for a fast, lossless mux.
        await RunAsync(
            ["-y", "-i", videoPath, "-i", audioPath, "-c", "copy", outputPath],
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ExtractAudioAsync(string inputPath, string outputPath, string? audioCodec = null, CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "-y", "-i", inputPath, "-vn" };
        if (!string.IsNullOrWhiteSpace(audioCodec))
        {
            args.Add("-c:a");
            args.Add(audioCodec);
        }

        args.Add(outputPath);
        await RunAsync(args, cancellationToken).ConfigureAwait(false);
    }

    public async Task ConvertAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default)
    {
        await RunAsync(["-y", "-i", inputPath, outputPath], cancellationToken).ConfigureAwait(false);
    }

    public async Task EmbedMetadataAsync(string inputPath, string outputPath, VideoInfo metadata, CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "-y", "-i", inputPath, "-c", "copy" };

        AddMetadata(args, "title", metadata.Title);
        AddMetadata(args, "artist", metadata.Uploader);
        AddMetadata(args, "comment", metadata.WebpageUrl);
        if (metadata.UploadDate is { } date)
        {
            AddMetadata(args, "date", date.ToString("yyyy-MM-dd"));
        }

        args.Add(outputPath);
        await RunAsync(args, cancellationToken).ConfigureAwait(false);
    }

    public async Task EmbedThumbnailAsync(string mediaPath, string thumbnailPath, string outputPath, CancellationToken cancellationToken = default)
    {
        // Attach the image as cover art. Works for mp4/m4a/mkv containers.
        await RunAsync(
        [
            "-y", "-i", mediaPath, "-i", thumbnailPath,
            "-map", "0", "-map", "1",
            "-c", "copy",
            "-disposition:v:1", "attached_pic",
            outputPath
        ], cancellationToken).ConfigureAwait(false);
    }

    public async Task EmbedChaptersAsync(string inputPath, string outputPath, IReadOnlyList<Chapter> chapters, CancellationToken cancellationToken = default)
    {
        if (chapters.Count == 0)
        {
            _logger.LogInformation("No chapters to embed; copying input to output.");
            await RunAsync(["-y", "-i", inputPath, "-c", "copy", outputPath], cancellationToken).ConfigureAwait(false);
            return;
        }

        // Write an ffmetadata file describing the chapters, then mux it in.
        var metadataFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(metadataFile, BuildChapterMetadata(chapters), cancellationToken).ConfigureAwait(false);

            await RunAsync(
            [
                "-y", "-i", inputPath, "-i", metadataFile,
                "-map_metadata", "1",
                "-c", "copy",
                outputPath
            ], cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            TryDelete(metadataFile);
        }
    }

    private static string BuildChapterMetadata(IReadOnlyList<Chapter> chapters)
    {
        var sb = new StringBuilder();
        sb.AppendLine(";FFMETADATA1");

        foreach (var chapter in chapters)
        {
            // ffmetadata expects integer timebase units; use milliseconds (1/1000).
            var start = (long)chapter.StartTime.TotalMilliseconds;
            var end = (long)chapter.EndTime.TotalMilliseconds;
            if (end <= start)
            {
                end = start + 1;
            }

            sb.AppendLine("[CHAPTER]");
            sb.AppendLine("TIMEBASE=1/1000");
            sb.AppendLine($"START={start}");
            sb.AppendLine($"END={end}");
            sb.AppendLine($"title={EscapeMetadataValue(chapter.Title)}");
        }

        return sb.ToString();
    }

    private static string EscapeMetadataValue(string value) =>
        value.Replace("\\", "\\\\").Replace("=", "\\=").Replace(";", "\\;").Replace("#", "\\#").Replace("\n", " ");

    private static void AddMetadata(List<string> args, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        args.Add("-metadata");
        args.Add($"{key}={value}");
    }

    private async Task RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var exe = await ResolveExecutableAsync(cancellationToken).ConfigureAwait(false);
        var result = await _processRunner.RunAsync(exe, args, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            var tail = result.StandardError.Length > 500 ? result.StandardError[^500..] : result.StandardError;
            _logger.LogError("ffmpeg exited with code {Code}", result.ExitCode);
            throw new FFmpegException(
                $"ffmpeg exited with code {result.ExitCode}.",
                userMessage: "Media processing failed.",
                exitCode: result.ExitCode,
                standardError: tail);
        }
    }

    private async Task<string> ResolveExecutableAsync(CancellationToken cancellationToken)
    {
        var path = await _dependencies.ResolvePathAsync("ffmpeg", cancellationToken).ConfigureAwait(false);
        if (path is null)
        {
            throw new DependencyException("ffmpeg", "ffmpeg executable was not found.",
                userMessage: "FFmpeg is not available. Set its path in Settings or install it.");
        }

        return path;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup of the temp metadata file.
        }
    }
}
