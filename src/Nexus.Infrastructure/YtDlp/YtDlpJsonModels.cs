using System.Text.Json.Serialization;

namespace Nexus.Infrastructure.YtDlp;

/// <summary>
/// Deserialization surface for yt-dlp's <c>-J</c> JSON output. Intentionally
/// internal to Infrastructure: these types mirror yt-dlp's schema and are mapped
/// into the app's own models by <see cref="YtDlpMapper"/>, so no other layer
/// couples to this shape.
/// </summary>
internal sealed class YtDlpRoot
{
    [JsonPropertyName("_type")]
    public string? Type { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("uploader")]
    public string? Uploader { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; }

    [JsonPropertyName("channel_url")]
    public string? ChannelUrl { get; set; }

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    [JsonPropertyName("upload_date")]
    public string? UploadDate { get; set; }

    [JsonPropertyName("view_count")]
    public long? ViewCount { get; set; }

    [JsonPropertyName("like_count")]
    public long? LikeCount { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("thumbnails")]
    public List<YtDlpThumbnail>? Thumbnails { get; set; }

    [JsonPropertyName("webpage_url")]
    public string? WebpageUrl { get; set; }

    [JsonPropertyName("original_url")]
    public string? OriginalUrl { get; set; }

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("chapters")]
    public List<YtDlpChapter>? Chapters { get; set; }

    [JsonPropertyName("formats")]
    public List<YtDlpFormat>? Formats { get; set; }

    [JsonPropertyName("subtitles")]
    public Dictionary<string, List<YtDlpSubtitle>>? Subtitles { get; set; }

    [JsonPropertyName("automatic_captions")]
    public Dictionary<string, List<YtDlpSubtitle>>? AutomaticCaptions { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("availability")]
    public string? Availability { get; set; }

    [JsonPropertyName("is_live")]
    public bool? IsLive { get; set; }

    [JsonPropertyName("comment_count")]
    public long? CommentCount { get; set; }

    [JsonPropertyName("channel_follower_count")]
    public long? ChannelFollowerCount { get; set; }

    [JsonPropertyName("age_limit")]
    public int? AgeLimit { get; set; }

    [JsonPropertyName("live_status")]
    public string? LiveStatus { get; set; }

    [JsonPropertyName("was_live")]
    public bool? WasLive { get; set; }

    [JsonPropertyName("uploader_id")]
    public string? UploaderId { get; set; }

    [JsonPropertyName("uploader_url")]
    public string? UploaderUrl { get; set; }

    [JsonPropertyName("license")]
    public string? License { get; set; }

    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; set; }

    [JsonPropertyName("release_timestamp")]
    public long? ReleaseTimestamp { get; set; }

    [JsonPropertyName("playable_in_embed")]
    public bool? PlayableInEmbed { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }

    [JsonPropertyName("track")]
    public string? Track { get; set; }

    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    [JsonPropertyName("album")]
    public string? Album { get; set; }

    // Playlist-specific
    [JsonPropertyName("entries")]
    public List<YtDlpRoot>? Entries { get; set; }

    [JsonPropertyName("playlist_count")]
    public int? PlaylistCount { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

internal sealed class YtDlpThumbnail
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("preference")]
    public int? Preference { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }
}

internal sealed class YtDlpChapter
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("start_time")]
    public double? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public double? EndTime { get; set; }
}

internal sealed class YtDlpFormat
{
    [JsonPropertyName("format_id")]
    public string? FormatId { get; set; }

    [JsonPropertyName("ext")]
    public string? Ext { get; set; }

    [JsonPropertyName("container")]
    public string? Container { get; set; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("fps")]
    public double? Fps { get; set; }

    [JsonPropertyName("vcodec")]
    public string? VideoCodec { get; set; }

    [JsonPropertyName("acodec")]
    public string? AudioCodec { get; set; }

    [JsonPropertyName("abr")]
    public double? AudioBitrate { get; set; }

    [JsonPropertyName("vbr")]
    public double? VideoBitrate { get; set; }

    [JsonPropertyName("tbr")]
    public double? TotalBitrate { get; set; }

    [JsonPropertyName("filesize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("filesize_approx")]
    public long? FileSizeApprox { get; set; }

    [JsonPropertyName("dynamic_range")]
    public string? DynamicRange { get; set; }

    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    [JsonPropertyName("quality")]
    public double? Quality { get; set; }

    [JsonPropertyName("format_note")]
    public string? FormatNote { get; set; }
}

internal sealed class YtDlpSubtitle
{
    [JsonPropertyName("ext")]
    public string? Ext { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
