using Nexus.Core.Enums;

namespace Nexus.Core.Models;

/// <summary>
/// The per-download choices a user makes on the Home page. Captured when a task
/// is created and used to assemble yt-dlp/FFmpeg arguments. Mutable by design so
/// the Home view-model can bind to it directly.
/// </summary>
public sealed class DownloadOptions
{
    public DownloadType DownloadType { get; set; } = DownloadType.Video;

    public QualityPreference Quality { get; set; } = QualityPreference.Best;

    public OutputContainer Container { get; set; } = OutputContainer.Auto;

    /// <summary>Explicit format id when the user picks from the format list (Quality = Custom).</summary>
    public string? CustomFormatId { get; set; }

    /// <summary>Custom container/extension when <see cref="Container"/> is Custom.</summary>
    public string? CustomContainer { get; set; }

    public bool DownloadThumbnail { get; set; }
    public bool EmbedThumbnail { get; set; }
    public bool DownloadSubtitles { get; set; }
    public bool DownloadAutomaticSubtitles { get; set; }

    /// <summary>Subtitle language codes to fetch. Empty means "all available" when subtitles are enabled.</summary>
    public IList<string> SubtitleLanguages { get; set; } = new List<string>();

    /// <summary>Preferred subtitle output format, e.g. "srt".</summary>
    public string? SubtitleFormat { get; set; }

    public bool EmbedSubtitles { get; set; }
    public bool EmbedMetadata { get; set; }
    public bool DownloadChapters { get; set; }
    public bool EmbedChapters { get; set; }

    /// <summary>Write the info JSON sidecar next to the media.</summary>
    public bool WriteInfoJson { get; set; }

    /// <summary>Output directory override. Falls back to the configured default when null.</summary>
    public string? OutputDirectory { get; set; }

    /// <summary>Output filename template override. Falls back to the configured default when null.</summary>
    public string? OutputTemplate { get; set; }

    /// <summary>
    /// Creates an independent copy. The Home view-model binds a single options
    /// instance; each enqueued task must capture its own snapshot so later edits
    /// don't mutate in-flight downloads.
    /// </summary>
    public DownloadOptions Clone()
    {
        var clone = (DownloadOptions)MemberwiseClone();
        clone.SubtitleLanguages = new List<string>(SubtitleLanguages);
        return clone;
    }
}
