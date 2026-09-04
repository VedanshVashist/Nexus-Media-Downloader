using FluentAssertions;
using Nexus.Core.Enums;
using Nexus.Core.Models;
using Nexus.Core.Utilities;
using Xunit;

namespace Nexus.Tests.Utilities;

public sealed class FormatFilterTests
{
    private static List<VideoFormat> SampleFormats() =>
    [
        new() { FormatId = "137", Height = 1080, Width = 1920, Fps = 30, VideoCodec = "avc1", AudioCodec = "none" },
        new() { FormatId = "248", Height = 1080, Width = 1920, Fps = 60, VideoCodec = "vp9", AudioCodec = "none" },
        new() { FormatId = "22", Height = 720, Width = 1280, VideoCodec = "avc1", AudioCodec = "mp4a" },
        new() { FormatId = "140", VideoCodec = "none", AudioCodec = "mp4a", AudioBitrate = 128 },
        new() { FormatId = "251", VideoCodec = "none", AudioCodec = "opus", AudioBitrate = 160 }
    ];

    [Fact]
    public void VideoFormats_ExcludesAudioOnly_AndRanksByHeightThenFps()
    {
        var video = FormatFilter.VideoFormats(SampleFormats());

        video.Should().HaveCount(3);
        video.Should().NotContain(f => f.IsAudioOnly);
        // 1080p60 should outrank 1080p30.
        video[0].FormatId.Should().Be("248");
    }

    [Fact]
    public void AudioFormats_OnlyAudio_RankedByBitrate()
    {
        var audio = FormatFilter.AudioFormats(SampleFormats());

        audio.Should().HaveCount(2);
        audio[0].FormatId.Should().Be("251"); // 160 kbps beats 128
    }

    [Fact]
    public void AvailableHeights_DistinctDescending()
    {
        FormatFilter.AvailableHeights(SampleFormats()).Should().ContainInOrder(1080, 720);
    }

    [Theory]
    [InlineData(QualityPreference.Best, "bestvideo*+bestaudio/best")]
    [InlineData(QualityPreference.P1080, "bestvideo[height<=1080]+bestaudio/best[height<=1080]/best")]
    [InlineData(QualityPreference.P720, "bestvideo[height<=720]+bestaudio/best[height<=720]/best")]
    public void BuildVideoSelector_MapsQuality(QualityPreference quality, string expected)
    {
        FormatFilter.BuildVideoSelector(quality).Should().Be(expected);
    }

    [Fact]
    public void BuildVideoSelector_CustomFormatIncludesAudioFallback()
    {
        FormatFilter.BuildVideoSelector(QualityPreference.Custom, "137")
            .Should().Be("137+bestaudio/137");
    }

    [Fact]
    public void BuildAudioSelector_DefaultsToBestAudio()
    {
        FormatFilter.BuildAudioSelector().Should().Be("bestaudio/best");
        FormatFilter.BuildAudioSelector("140").Should().Be("140");
    }

    [Theory]
    [InlineData(OutputContainer.Mp4, "mp4", false)]
    [InlineData(OutputContainer.Mp3, "mp3", true)]
    [InlineData(OutputContainer.M4a, "m4a", true)]
    [InlineData(OutputContainer.Auto, null, false)]
    public void ContainerExtension_AndAudioDetection(OutputContainer container, string? expectedExt, bool isAudio)
    {
        FormatFilter.ContainerExtension(container).Should().Be(expectedExt);
        FormatFilter.IsAudioContainer(container).Should().Be(isAudio);
    }
}
