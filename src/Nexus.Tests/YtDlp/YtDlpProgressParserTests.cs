using FluentAssertions;
using Nexus.Core.Enums;
using Nexus.Infrastructure.YtDlp;
using Xunit;

namespace Nexus.Tests.YtDlp;

public sealed class YtDlpProgressParserTests
{
    private static readonly Guid TaskId = Guid.NewGuid();

    [Fact]
    public void TryParse_ParsesDownloadingLine()
    {
        const string line = "NEXUS_PROGRESS downloading 5000 10000 NA 250000 20";

        var progress = YtDlpProgressParser.TryParse(line, TaskId);

        progress.Should().NotBeNull();
        progress!.Status.Should().Be(DownloadStatus.Downloading);
        progress.DownloadedBytes.Should().Be(5000);
        progress.TotalBytes.Should().Be(10000);
        progress.Percent.Should().BeApproximately(50, 0.01);
        progress.SpeedBytesPerSecond.Should().Be(250000);
        progress.Eta.Should().Be(TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void TryParse_UsesEstimateWhenTotalUnknown()
    {
        const string line = "NEXUS_PROGRESS downloading 2500 NA 10000 100000 5";

        var progress = YtDlpProgressParser.TryParse(line, TaskId);

        progress.Should().NotBeNull();
        progress!.TotalBytes.Should().Be(10000);
        progress.Percent.Should().BeApproximately(25, 0.01);
    }

    [Fact]
    public void TryParse_FinishedMapsToProcessing()
    {
        const string line = "NEXUS_PROGRESS finished 10000 10000 NA NA NA";

        var progress = YtDlpProgressParser.TryParse(line, TaskId);

        progress.Should().NotBeNull();
        progress!.Status.Should().Be(DownloadStatus.Processing);
    }

    [Theory]
    [InlineData("")]
    [InlineData("some random yt-dlp log line")]
    [InlineData("[download] 50% of 10MiB")]
    public void TryParse_NonProgressLinesReturnNull(string line)
    {
        YtDlpProgressParser.TryParse(line, TaskId).Should().BeNull();
    }
}
