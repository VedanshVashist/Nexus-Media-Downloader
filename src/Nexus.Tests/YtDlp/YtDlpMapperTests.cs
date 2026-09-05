using System.Text.Json;
using FluentAssertions;
using Nexus.Infrastructure.YtDlp;
using Xunit;

namespace Nexus.Tests.YtDlp;

/// <summary>
/// Exercises the yt-dlp JSON → VideoInfo/PlaylistInfo mapping using representative
/// (trimmed) yt-dlp output. Relies on InternalsVisibleTo for the internal mapper.
/// </summary>
public sealed class YtDlpMapperTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private const string SingleVideoJson = """
    {
      "_type": "video",
      "id": "dQw4w9WgXcQ",
      "title": "Test Video",
      "description": "A description",
      "uploader": "Test Channel",
      "channel_id": "UC123",
      "duration": 212,
      "upload_date": "20091025",
      "view_count": 1500000,
      "like_count": 42000,
      "webpage_url": "https://youtube.com/watch?v=dQw4w9WgXcQ",
      "thumbnails": [
        { "url": "https://img/low.jpg", "width": 120, "height": 90, "preference": -1 },
        { "url": "https://img/high.jpg", "width": 1280, "height": 720, "preference": 0 }
      ],
      "categories": ["Music"],
      "tags": ["a", "b"],
      "chapters": [
        { "title": "Intro", "start_time": 0, "end_time": 30 },
        { "title": "Verse", "start_time": 30, "end_time": 90 }
      ],
      "formats": [
        { "format_id": "137", "ext": "mp4", "height": 1080, "width": 1920, "fps": 30, "vcodec": "avc1", "acodec": "none", "filesize": 50000000 },
        { "format_id": "140", "ext": "m4a", "vcodec": "none", "acodec": "mp4a", "abr": 128 }
      ],
      "subtitles": { "en": [ { "ext": "vtt", "url": "https://s/en.vtt" }, { "ext": "srt", "url": "https://s/en.srt" } ] },
      "automatic_captions": { "en": [ { "ext": "vtt" } ], "fr": [ { "ext": "vtt" } ] },
      "language": "en",
      "availability": "public"
    }
    """;

    private const string PlaylistJson = """
    {
      "_type": "playlist",
      "id": "PL123",
      "title": "My Playlist",
      "uploader": "Playlist Owner",
      "entries": [
        { "id": "v1", "title": "First", "url": "https://youtube.com/watch?v=v1", "duration": 100 },
        { "id": "v2", "title": "Second", "url": "https://youtube.com/watch?v=v2", "duration": 200 }
      ]
    }
    """;

    private const string ExtendedVideoJson = """
    {
      "_type": "video",
      "id": "abc123",
      "title": "Extended Video",
      "uploader": "Music Channel",
      "uploader_id": "@musicchannel",
      "uploader_url": "https://youtube.com/@musicchannel",
      "channel_follower_count": 2500000,
      "comment_count": 12000,
      "view_count": 9000000,
      "like_count": 300000,
      "age_limit": 18,
      "live_status": "was_live",
      "was_live": true,
      "license": "Standard YouTube License",
      "timestamp": 1700000000,
      "release_timestamp": 1699000000,
      "playable_in_embed": false,
      "width": 1920,
      "height": 1080,
      "resolution": "1920x1080",
      "track": "Never Gonna Give You Up",
      "artist": "Rick Astley",
      "album": "Whenever You Need Somebody"
    }
    """;

    [Fact]
    public void ToVideoInfo_MapsCoreFields()
    {
        var root = JsonSerializer.Deserialize<YtDlpRoot>(SingleVideoJson, Options)!;

        YtDlpMapper.IsPlaylist(root).Should().BeFalse();
        var info = YtDlpMapper.ToVideoInfo(root);

        info.Id.Should().Be("dQw4w9WgXcQ");
        info.Title.Should().Be("Test Video");
        info.Uploader.Should().Be("Test Channel");
        info.Duration.Should().Be(TimeSpan.FromSeconds(212));
        info.UploadDate.Should().Be(new DateOnly(2009, 10, 25));
        info.ViewCount.Should().Be(1500000);
    }

    [Fact]
    public void ToVideoInfo_SelectsHighestResolutionThumbnail()
    {
        var root = JsonSerializer.Deserialize<YtDlpRoot>(SingleVideoJson, Options)!;
        var info = YtDlpMapper.ToVideoInfo(root);
        info.ThumbnailUrl.Should().Be("https://img/high.jpg");
    }

    [Fact]
    public void ToVideoInfo_MapsChaptersFormatsAndSubtitles()
    {
        var root = JsonSerializer.Deserialize<YtDlpRoot>(SingleVideoJson, Options)!;
        var info = YtDlpMapper.ToVideoInfo(root);

        info.Chapters.Should().HaveCount(2);
        info.Chapters[0].Title.Should().Be("Intro");
        info.Chapters[0].Duration.Should().Be(TimeSpan.FromSeconds(30));

        info.Formats.Should().HaveCount(2);
        info.Formats.Should().Contain(f => f.FormatId == "137" && f.IsVideoOnly);
        info.Formats.Should().Contain(f => f.FormatId == "140" && f.IsAudioOnly);

        // English has a manual track, so the automatic 'en' is skipped; 'fr' auto remains.
        info.Subtitles.Should().Contain(s => s.LanguageCode == "en" && !s.IsAutomatic);
        info.Subtitles.Should().Contain(s => s.LanguageCode == "fr" && s.IsAutomatic);
        info.Subtitles.Should().NotContain(s => s.LanguageCode == "en" && s.IsAutomatic);
    }

    [Fact]
    public void ToPlaylistInfo_MapsEntriesInOrder()
    {
        var root = JsonSerializer.Deserialize<YtDlpRoot>(PlaylistJson, Options)!;

        YtDlpMapper.IsPlaylist(root).Should().BeTrue();
        var playlist = YtDlpMapper.ToPlaylistInfo(root);

        playlist.Title.Should().Be("My Playlist");
        playlist.Count.Should().Be(2);
        playlist.Entries[0].Index.Should().Be(1);
        playlist.Entries[0].Title.Should().Be("First");
        playlist.Entries[1].Index.Should().Be(2);
    }

    [Fact]
    public void ParseUploadDate_HandlesInvalid()
    {
        YtDlpMapper.ParseUploadDate("notadate").Should().BeNull();
        YtDlpMapper.ParseUploadDate(null).Should().BeNull();
        YtDlpMapper.ParseUploadDate("20240501").Should().Be(new DateOnly(2024, 5, 1));
    }

    [Fact]
    public void ToVideoInfo_MapsExtendedMetadata()
    {
        var root = JsonSerializer.Deserialize<YtDlpRoot>(ExtendedVideoJson, Options)!;

        YtDlpMapper.IsPlaylist(root).Should().BeFalse();
        var info = YtDlpMapper.ToVideoInfo(root);

        info.SubscriberCount.Should().Be(2500000);
        info.CommentCount.Should().Be(12000);
        info.AgeLimit.Should().Be(18);
        info.HasAgeRestriction.Should().BeTrue();
        info.LiveStatus.Should().Be("was_live");
        info.WasLive.Should().BeTrue();
        info.UploaderId.Should().Be("@musicchannel");
        info.UploaderUrl.Should().Be("https://youtube.com/@musicchannel");
        info.License.Should().Be("Standard YouTube License");
        info.PlayableInEmbed.Should().BeFalse();
        info.Width.Should().Be(1920);
        info.Height.Should().Be(1080);
        info.Resolution.Should().Be("1920x1080");
        info.Track.Should().Be("Never Gonna Give You Up");
        info.Artist.Should().Be("Rick Astley");
        info.Album.Should().Be("Whenever You Need Somebody");
        info.IsMusic.Should().BeTrue();

        // timestamp is preferred over release_timestamp.
        info.PublishedAt.Should().NotBeNull();
        info.PublishedAt!.Value.ToUnixTimeSeconds().Should().Be(1700000000);
    }

    [Fact]
    public void FromUnixSeconds_HandlesNullAndZero()
    {
        YtDlpMapper.FromUnixSeconds(null).Should().BeNull();
        YtDlpMapper.FromUnixSeconds(0).Should().BeNull();
        YtDlpMapper.FromUnixSeconds(1700000000).Should().NotBeNull();
        YtDlpMapper.FromUnixSeconds(1700000000)!.Value.ToUnixTimeSeconds().Should().Be(1700000000);
    }
}
