using Nexus.Core.Enums;
using Nexus.Core.Models;
using Nexus.Core.Utilities;

namespace Nexus.Infrastructure.YtDlp;

/// <summary>
/// Assembles yt-dlp argument lists from strongly typed options. Every value is
/// added as a discrete list element (never concatenated into a command line), so
/// user-controlled titles, paths, and URLs cannot inject arguments or shell
/// syntax. Custom user arguments are tokenized with a quote-aware splitter.
/// </summary>
public static class YtDlpArgumentBuilder
{
    /// <summary>Arguments to extract full metadata for a single URL as JSON.</summary>
    public static IReadOnlyList<string> BuildInfoArguments(string url)
    {
        return
        [
            "--dump-single-json",
            "--no-playlist",
            "--no-warnings",
            "--no-progress",
            url
        ];
    }

    /// <summary>Arguments to analyze a URL, using flat extraction so playlists stay fast.</summary>
    public static IReadOnlyList<string> BuildAnalyzeArguments(string url)
    {
        return
        [
            "--dump-single-json",
            "--flat-playlist",
            "--no-warnings",
            "--no-progress",
            url
        ];
    }

    /// <summary>Arguments to print the yt-dlp version.</summary>
    public static IReadOnlyList<string> BuildVersionArguments() => ["--version"];

    /// <summary>
    /// Builds the full download argument list for a task. Emits machine-readable
    /// progress lines that <see cref="YtDlpProgressParser"/> consumes.
    /// </summary>
    /// <param name="task">The task to download.</param>
    /// <param name="settings">Application settings supplying paths, tool config, and templates.</param>
    /// <param name="outputDirectory">Resolved, sanitized destination directory.</param>
    /// <param name="ffmpegLocation">Optional ffmpeg directory to pass to yt-dlp.</param>
    public static IReadOnlyList<string> BuildDownloadArguments(
        DownloadTask task,
        AppSettings settings,
        string outputDirectory,
        string? ffmpegLocation)
    {
        var options = task.Options;
        var args = new List<string>
        {
            // Emit a stable, parseable progress template we control.
            "--newline",
            "--progress-template",
            "download:NEXUS_PROGRESS %(progress.status)s %(progress.downloaded_bytes)s %(progress.total_bytes)s %(progress.total_bytes_estimate)s %(progress.speed)s %(progress.eta)s"
        };

        // Output template: yt-dlp performs the final sanitization via --restrict-filenames off,
        // but we constrain the directory and provide a template mapped from the user's.
        var ytdlpTemplate = ToYtDlpOutputTemplate(options.OutputTemplate ?? settings.Downloads.OutputTemplate);
        args.Add("-o");
        args.Add(Path.Combine(outputDirectory, ytdlpTemplate));

        if (settings.Downloads.OverwriteExisting)
        {
            args.Add("--force-overwrites");
        }
        else
        {
            args.Add("--no-overwrites");
        }

        if (!string.IsNullOrWhiteSpace(ffmpegLocation))
        {
            args.Add("--ffmpeg-location");
            args.Add(ffmpegLocation);
        }

        BuildFormatArguments(args, task);
        BuildContentArguments(args, options);

        // Append safely tokenized custom user arguments last.
        foreach (var token in ArgumentTokenizer.Tokenize(settings.YtDlp.CustomArguments))
        {
            args.Add(token);
        }

        args.Add(task.Url);
        return args;
    }

    private static void BuildFormatArguments(List<string> args, DownloadTask task)
    {
        var options = task.Options;

        if (options.DownloadType == DownloadType.Audio)
        {
            args.Add("-f");
            args.Add(FormatFilter.BuildAudioSelector(options.CustomFormatId));
            args.Add("--extract-audio");

            var audioExt = FormatFilter.ContainerExtension(options.Container, options.CustomContainer);
            if (audioExt is not null && FormatFilter.IsAudioContainer(options.Container))
            {
                args.Add("--audio-format");
                args.Add(audioExt);
            }

            return;
        }

        // Video (default).
        args.Add("-f");
        args.Add(FormatFilter.BuildVideoSelector(options.Quality, options.CustomFormatId));

        var container = FormatFilter.ContainerExtension(options.Container, options.CustomContainer);
        if (container is not null && !FormatFilter.IsAudioContainer(options.Container))
        {
            args.Add("--merge-output-format");
            args.Add(container);
        }
    }

    private static void BuildContentArguments(List<string> args, DownloadOptions options)
    {
        if (options.DownloadThumbnail)
        {
            args.Add("--write-thumbnail");
        }

        if (options.EmbedThumbnail)
        {
            args.Add("--embed-thumbnail");
        }

        if (options.DownloadSubtitles || options.DownloadAutomaticSubtitles)
        {
            if (options.DownloadSubtitles)
            {
                args.Add("--write-subs");
            }

            if (options.DownloadAutomaticSubtitles)
            {
                args.Add("--write-auto-subs");
            }

            args.Add("--sub-langs");
            args.Add(options.SubtitleLanguages.Count > 0 ? string.Join(",", options.SubtitleLanguages) : "all");

            if (!string.IsNullOrWhiteSpace(options.SubtitleFormat))
            {
                args.Add("--sub-format");
                args.Add(options.SubtitleFormat);
            }
        }

        if (options.EmbedSubtitles)
        {
            args.Add("--embed-subs");
        }

        if (options.EmbedMetadata)
        {
            args.Add("--embed-metadata");
        }

        if (options.DownloadChapters)
        {
            args.Add("--write-info-json");
        }

        if (options.EmbedChapters)
        {
            args.Add("--embed-chapters");
        }

        if (options.WriteInfoJson)
        {
            args.Add("--write-info-json");
        }
    }

    /// <summary>
    /// Translates the app's friendly template tokens (e.g. {title}, {id}, {ext})
    /// into yt-dlp's own output-template syntax (%(title)s, %(id)s, %(ext)s).
    /// Unknown tokens are passed through untouched.
    /// </summary>
    internal static string ToYtDlpOutputTemplate(string appTemplate)
    {
        if (string.IsNullOrWhiteSpace(appTemplate))
        {
            appTemplate = "{title} [{id}].{ext}";
        }

        return appTemplate
            .Replace("{title}", "%(title)s")
            .Replace("{channel}", "%(channel,uploader)s")
            .Replace("{uploader}", "%(uploader)s")
            .Replace("{upload_date}", "%(upload_date)s")
            .Replace("{id}", "%(id)s")
            .Replace("{resolution}", "%(resolution)s")
            .Replace("{ext}", "%(ext)s");
    }
}
