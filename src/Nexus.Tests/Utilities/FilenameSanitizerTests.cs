using FluentAssertions;
using Nexus.Core.Utilities;
using Xunit;

namespace Nexus.Tests.Utilities;

public sealed class FilenameSanitizerTests
{
    [Theory]
    [InlineData("normal title", "normal title")]
    [InlineData("a/b\\c:d*e?f", "a_b_c_d_e_f")]
    [InlineData("has\"quotes\"", "has_quotes_")]
    [InlineData("pipe|here", "pipe_here")]
    public void SanitizeComponent_ReplacesInvalidCharacters(string input, string expected)
    {
        FilenameSanitizer.SanitizeComponent(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("...")]
    public void SanitizeComponent_FallsBackForEmptyOrDotOnly(string input)
    {
        FilenameSanitizer.SanitizeComponent(input).Should().Be("untitled");
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("nul")]
    [InlineData("COM1")]
    [InlineData("LPT9")]
    public void SanitizeComponent_EscapesReservedDeviceNames(string reserved)
    {
        var result = FilenameSanitizer.SanitizeComponent(reserved);
        result.Should().NotBe(reserved);
        result.Should().StartWith("_");
    }

    [Fact]
    public void SanitizeComponent_TrimsTrailingDotsAndSpaces()
    {
        FilenameSanitizer.SanitizeComponent("filename.  ").Should().Be("filename");
    }

    [Fact]
    public void SanitizeComponent_BoundsLength()
    {
        var longName = new string('a', 500);
        FilenameSanitizer.SanitizeComponent(longName).Length.Should().BeLessThanOrEqualTo(200);
    }

    [Fact]
    public void IsWithinDirectory_AllowsPathsInside()
    {
        var baseDir = OperatingSystem.IsWindows() ? @"C:\downloads" : "/downloads";
        var inside = Path.Combine(baseDir, "video.mp4");
        FilenameSanitizer.IsWithinDirectory(baseDir, inside).Should().BeTrue();
    }

    [Fact]
    public void IsWithinDirectory_BlocksTraversal()
    {
        var baseDir = OperatingSystem.IsWindows() ? @"C:\downloads" : "/downloads";
        var outside = Path.Combine(baseDir, "..", "..", "etc", "passwd");
        FilenameSanitizer.IsWithinDirectory(baseDir, outside).Should().BeFalse();
    }
}
