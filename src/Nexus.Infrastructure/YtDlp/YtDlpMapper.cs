using System.Globalization;
using Nexus.Core.Models;

namespace Nexus.Infrastructure.YtDlp;

/// <summary>
/// Converts yt-dlp's raw JSON (<see cref="YtDlpRoot"/>) into the application's own
/// strongly typed models. This is the single seam where the external schema is
/// tolerated; everything upstream depends only on <see cref="VideoInfo"/> etc.
/// </summary>
internal static class YtDlpMapper
{
    /// <summary>True when the parsed root represents a playlist/multi_video.</summary>
    public static bool IsPlaylist(YtDlpRoot root) =>
        string.Equals(root.Type, "playlist", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(root.Type, "multi_video", StringComparison.OrdinalIgnoreCase) ||
        (root.Entries is { Count: > 0 } && root.Formats is null or { Count: 0 });

    public static VideoInfo ToVideoInfo(YtDlpRoot root)
    {
        return new VideoInfo
        {
            Id = root.Id ?? string.Empty,
            Title = string.IsNullOrWhiteSpace(root.Title) ? "Untitled" : root.Title,
            Description = root.Description,
            Uploader = root.Uploader ?? root.Channel,
            ChannelId = root.ChannelId,
            ChannelUrl = root.ChannelUrl,
            Duration = root.Duration is > 0 ? TimeSpan.FromSeconds(root.Duration.Value) : TimeSpan.Zero,
            UploadDate = ParseUploadDate(root.UploadDate),
            ViewCount = root.ViewCount,
            LikeCount = root.LikeCount,
            ThumbnailUrl = SelectBestThumbnail(root),
            WebpageUrl = root.WebpageUrl,
            OriginalUrl = root.OriginalUrl ?? root.WebpageUrl,
            Categories = root.Categories ?? [],
            Tags = root.Tags ?? [],
            Chapters = MapChapters(root.Chapters),
            Formats = MapFormats(root.Formats),
            Subtitles = MapSubtitles(root.Subtitles, root.AutomaticCaptions),
            Language = root.Language,
            Availability = root.Availability,
            IsLive = root.IsLive ?? false
        };
    }

    public static PlaylistInfo ToPlaylistInfo(YtDlpRoot root)
    {
        var entries = new List<PlaylistEntry>();
        if (root.Entries is not null)
        {
            var index = 1;
            foreach (var entry in root.Entries)
            {
                if (entry is null)
                {
                    continue;
                }

                entries.Add(new PlaylistEntry
                {
                    Index = index++,
                    Id = entry.Id ?? string.Empty,
                    Title = string.IsNullOrWhiteSpace(entry.Title) ? "Untitled" : entry.Title,
                    Url = entry.Url ?? entry.WebpageUrl,
                    ThumbnailUrl = SelectBestThumbnail(entry),
                    Duration = entry.Duration is > 0 ? TimeSpan.FromSeconds(entry.Duration.Value) : null
                });
            }
        }

        return new PlaylistInfo
        {
            Id = root.Id ?? string.Empty,
            Title = string.IsNullOrWhiteSpace(root.Title) ? "Playlist" : root.Title,
            Uploader = root.Uploader ?? root.Channel,
            ChannelId = root.ChannelId,
            WebpageUrl = root.WebpageUrl,
            ThumbnailUrl = SelectBestThumbnail(root),
            Entries = entries
        };
    }

    internal static DateOnly? ParseUploadDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        // yt-dlp emits YYYYMMDD.
        if (DateOnly.TryParseExact(raw, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        return null;
    }

    internal static string? SelectBestThumbnail(YtDlpRoot root)
    {
        if (root.Thumbnails is { Count: > 0 })
        {
            // Prefer the highest-resolution thumbnail; fall back to yt-dlp's preference.
            var best = root.Thumbnails
                .Where(t => !string.IsNullOrWhiteSpace(t.Url))
                .OrderByDescending(t => (long)(t.Width ?? 0) * (t.Height ?? 0))
                .ThenByDescending(t => t.Preference ?? int.MinValue)
                .FirstOrDefault();

            if (best?.Url is not null)
            {
                return best.Url;
            }
        }

        return root.Thumbnail;
    }

    private static IReadOnlyList<Chapter> MapChapters(List<YtDlpChapter>? chapters)
    {
        if (chapters is null or { Count: 0 })
        {
            return [];
        }

        var result = new List<Chapter>(chapters.Count);
        foreach (var c in chapters)
        {
            result.Add(new Chapter
            {
                Title = string.IsNullOrWhiteSpace(c.Title) ? "Chapter" : c.Title,
                StartTime = TimeSpan.FromSeconds(c.StartTime ?? 0),
                EndTime = TimeSpan.FromSeconds(c.EndTime ?? c.StartTime ?? 0)
            });
        }

        return result;
    }

    private static IReadOnlyList<VideoFormat> MapFormats(List<YtDlpFormat>? formats)
    {
        if (formats is null or { Count: 0 })
        {
            return [];
        }

        var result = new List<VideoFormat>(formats.Count);
        foreach (var f in formats)
        {
            if (string.IsNullOrWhiteSpace(f.FormatId))
            {
                continue;
            }

            result.Add(new VideoFormat
            {
                FormatId = f.FormatId,
                Extension = f.Ext,
                Container = f.Container,
                Resolution = f.Resolution,
                Width = f.Width,
                Height = f.Height,
                Fps = f.Fps,
                VideoCodec = f.VideoCodec,
                AudioCodec = f.AudioCodec,
                AudioBitrate = f.AudioBitrate,
                VideoBitrate = f.VideoBitrate ?? f.TotalBitrate,
                FileSize = f.FileSize,
                FileSizeApproximation = f.FileSizeApprox,
                DynamicRange = f.DynamicRange,
                Protocol = f.Protocol,
                Quality = f.Quality,
                FormatNote = f.FormatNote
            });
        }

        return result;
    }

    private static IReadOnlyList<SubtitleInfo> MapSubtitles(
        Dictionary<string, List<YtDlpSubtitle>>? manual,
        Dictionary<string, List<YtDlpSubtitle>>? automatic)
    {
        var result = new List<SubtitleInfo>();

        if (manual is not null)
        {
            foreach (var (code, tracks) in manual)
            {
                result.Add(new SubtitleInfo
                {
                    Language = tracks.FirstOrDefault()?.Name ?? code,
                    LanguageCode = code,
                    IsAutomatic = false,
                    Formats = tracks.Select(t => t.Ext ?? "").Where(e => e.Length > 0).Distinct().ToList()
                });
            }
        }

        if (automatic is not null)
        {
            foreach (var (code, tracks) in automatic)
            {
                // Avoid duplicating a language already offered as a manual track.
                if (result.Any(s => string.Equals(s.LanguageCode, code, StringComparison.OrdinalIgnoreCase) && !s.IsAutomatic))
                {
                    continue;
                }

                result.Add(new SubtitleInfo
                {
                    Language = tracks.FirstOrDefault()?.Name ?? code,
                    LanguageCode = code,
                    IsAutomatic = true,
                    Formats = tracks.Select(t => t.Ext ?? "").Where(e => e.Length > 0).Distinct().ToList()
                });
            }
        }

        return result;
    }
}
