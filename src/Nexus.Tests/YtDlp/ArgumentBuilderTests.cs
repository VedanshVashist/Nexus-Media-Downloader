using FluentAssertions;
using Nexus.Core.Enums;
using Nexus.Core.Models;
using Nexus.Infrastructure.YtDlp;
using Xunit;

namespace Nexus.Tests.YtDlp;

public sealed class ArgumentBuilderTests
{
    [Fact]
    public void BuildInfoArguments_UsesJsonAndNoPlaylist()
    {
        var args = YtDlpArgumentBuilder.BuildInfoArguments("https://x.com/v");
        args.Should().Contain("--dump-single-json");
        args.Should().Contain("--no-playlist");
        args[^1].Should().Be("https://x.com/v");
    }

    [Fact]
    public void BuildDownloadArguments_VideoIncludesFormatSelectorAndMerge()
    {
        var task = new DownloadTask
        {
            Url = "https://x.com/v",
            DownloadType = DownloadType.Video,
            Options = new DownloadOptions
            {
                DownloadType = DownloadType.Video,
                Quality = QualityPreference.P1080,
                Container = OutputContainer.Mp4
            }
        };
        var settings = new AppSettings();

        var args = YtDlpArgumentBuilder.BuildDownloadArguments(task, settings, "/out", ffmpegLocation: null);

        args.Should().Contain("-f");
        args.Should().Contain("--merge-output-format");
        args.Should().Contain("mp4");
        // URL always last so nothing can be appended after it.
        args[^1].Should().Be("https://x.com/v");
    }

    [Fact]
    public void BuildDownloadArguments_AudioAddsExtractAudio()
    {
        var task = new DownloadTask
        {
            Url = "https://x.com/v",
            DownloadType = DownloadType.Audio,
            Options = new DownloadOptions
            {
                DownloadType = DownloadType.Audio,
                Container = OutputContainer.Mp3
            }
        };

        var args = YtDlpArgumentBuilder.BuildDownloadArguments(task, new AppSettings(), "/out", null);

        args.Should().Contain("--extract-audio");
        args.Should().Contain("--audio-format");
        args.Should().Contain("mp3");
    }

    [Fact]
    public void BuildDownloadArguments_EmbedsOptionsAsFlags()
    {
        var task = new DownloadTask
        {
            Url = "https://x.com/v",
            Options = new DownloadOptions
            {
                DownloadThumbnail = true,
                EmbedThumbnail = true,
                DownloadSubtitles = true,
                SubtitleLanguages = { "en", "fr" },
                EmbedMetadata = true,
                EmbedChapters = true
            }
        };

        var args = YtDlpArgumentBuilder.BuildDownloadArguments(task, new AppSettings(), "/out", null);

        args.Should().Contain("--write-thumbnail");
        args.Should().Contain("--embed-thumbnail");
        args.Should().Contain("--write-subs");
        args.Should().Contain("--sub-langs");
        args.Should().Contain("en,fr");
        args.Should().Contain("--embed-metadata");
        args.Should().Contain("--embed-chapters");
    }

    [Fact]
    public void ToYtDlpOutputTemplate_TranslatesTokens()
    {
        var result = YtDlpArgumentBuilder.ToYtDlpOutputTemplate("{title} [{id}].{ext}");
        result.Should().Be("%(title)s [%(id)s].%(ext)s");
    }
}
