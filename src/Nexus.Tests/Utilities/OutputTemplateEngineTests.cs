using FluentAssertions;
using Nexus.Core.Models;
using Nexus.Core.Utilities;
using Xunit;

namespace Nexus.Tests.Utilities;

public sealed class OutputTemplateEngineTests
{
    private static VideoInfo SampleVideo() => new()
    {
        Id = "abc123",
        Title = "My Great Video",
        Uploader = "Cool Channel",
        UploadDate = new DateOnly(2024, 5, 1)
    };

    [Fact]
    public void Render_SubstitutesAllTokens()
    {
        var result = OutputTemplateEngine.Render("{title} [{id}].{ext}", SampleVideo(), "mp4");
        result.Should().Be("My Great Video [abc123].mp4");
    }

    [Fact]
    public void Render_IncludesChannelAndDate()
    {
        var result = OutputTemplateEngine.Render("{upload_date} - {channel} - {title}.{ext}", SampleVideo(), "mkv");
        result.Should().Be("2024-05-01 - Cool Channel - My Great Video.mkv");
    }

    [Fact]
    public void Render_SanitizesIllegalCharactersFromTitle()
    {
        var video = SampleVideo() with { Title = "a/b:c" };
        var result = OutputTemplateEngine.Render("{title}.{ext}", video, "mp4");
        result.Should().NotContainAny("/", ":", "\\");
    }

    [Fact]
    public void Render_UnknownTokenLeftLiteral()
    {
        var result = OutputTemplateEngine.Render("{title}-{bogus}.{ext}", SampleVideo(), "mp4");
        result.Should().Contain("{bogus}");
    }

    [Fact]
    public void Render_EmptyTemplateUsesDefault()
    {
        var result = OutputTemplateEngine.Render("", SampleVideo(), "mp4");
        result.Should().Be("My Great Video [abc123].mp4");
    }
}
