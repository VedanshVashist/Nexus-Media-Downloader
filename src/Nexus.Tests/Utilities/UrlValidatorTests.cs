using FluentAssertions;
using Nexus.Core.Utilities;
using Xunit;

namespace Nexus.Tests.Utilities;

public sealed class UrlValidatorTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("http://youtube.com/watch?v=abc")]
    [InlineData("https://vimeo.com/12345")]
    public void IsValid_AcceptsWellFormedHttpUrls(string url)
    {
        UrlValidator.IsValid(url, out var normalized).Should().BeTrue();
        normalized.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/file")]
    [InlineData("file:///c:/secret.txt")]
    [InlineData("javascript:alert(1)")]
    [InlineData("localhost")]
    public void IsValid_RejectsInvalidOrUnsupported(string? url)
    {
        UrlValidator.IsValid(url, out var normalized).Should().BeFalse();
        normalized.Should().BeNull();
    }

    [Fact]
    public void ExtractUrls_PullsDistinctUrlsFromText_PreservingOrder()
    {
        const string text = """
            Check https://youtube.com/watch?v=1 and
            https://youtube.com/watch?v=2
            https://youtube.com/watch?v=1  (duplicate)
            garbage line
            https://vimeo.com/3
            """;

        var urls = UrlValidator.ExtractUrls(text);

        urls.Should().HaveCount(3);
        urls[0].Should().Contain("v=1");
        urls[1].Should().Contain("v=2");
        urls[2].Should().Contain("vimeo.com/3");
    }

    [Fact]
    public void ExtractUrls_EmptyInput_ReturnsEmpty()
    {
        UrlValidator.ExtractUrls("").Should().BeEmpty();
        UrlValidator.ExtractUrls(null).Should().BeEmpty();
    }
}
