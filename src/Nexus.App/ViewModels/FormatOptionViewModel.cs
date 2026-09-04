using System.Globalization;
using Nexus.Core.Models;

namespace Nexus.App.ViewModels;

/// <summary>
/// Presents a single <see cref="VideoFormat"/> as a selectable row in the format
/// picker, with human-readable primary and secondary labels. Purely a display
/// wrapper; the underlying <see cref="Format"/> is what gets applied to the task.
/// </summary>
public sealed class FormatOptionViewModel
{
    public FormatOptionViewModel(VideoFormat format)
    {
        Format = format;
        PrimaryLabel = BuildPrimaryLabel(format);
        SecondaryLabel = BuildSecondaryLabel(format);
    }

    public VideoFormat Format { get; }

    /// <summary>The format id used when this row is chosen.</summary>
    public string FormatId => Format.FormatId;

    /// <summary>Short label, e.g. "1080p60" or "audio · opus".</summary>
    public string PrimaryLabel { get; }

    /// <summary>Detail line, e.g. "mp4 · avc1 · 12.4 MB".</summary>
    public string SecondaryLabel { get; }

    private static string BuildPrimaryLabel(VideoFormat format)
    {
        if (format.IsAudioOnly)
        {
            var kbps = format.AudioBitrate is > 0
                ? $"{Math.Round(format.AudioBitrate.Value).ToString(CultureInfo.InvariantCulture)} kbps"
                : "audio";
            return $"Audio · {kbps}";
        }

        if (format.Height is > 0)
        {
            var line = $"{format.Height.Value.ToString(CultureInfo.InvariantCulture)}p";
            if (format.Fps is >= 50)
            {
                line += Math.Round(format.Fps.Value).ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(format.DynamicRange) &&
                !format.DynamicRange.Equals("SDR", StringComparison.OrdinalIgnoreCase))
            {
                line += $" {format.DynamicRange}";
            }

            return line;
        }

        return string.IsNullOrWhiteSpace(format.FormatNote) ? format.FormatId : format.FormatNote!;
    }

    private static string BuildSecondaryLabel(VideoFormat format)
    {
        var parts = new List<string>(4);

        if (!string.IsNullOrWhiteSpace(format.Extension))
        {
            parts.Add(format.Extension!);
        }

        var codec = format.IsAudioOnly ? format.AudioCodec : format.VideoCodec;
        if (!string.IsNullOrWhiteSpace(codec) && codec != "none")
        {
            // Trim codec profile suffix ("avc1.640028" → "avc1") for readability.
            var dot = codec!.IndexOf('.');
            parts.Add(dot > 0 ? codec[..dot] : codec);
        }

        var size = format.EffectiveFileSize;
        if (size is > 0)
        {
            parts.Add(HumanSize(size.Value));
        }

        parts.Add($"#{format.FormatId}");
        return string.Join(" · ", parts);
    }

    private static string HumanSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value.ToString(value >= 10 ? "0.#" : "0.##", CultureInfo.InvariantCulture)} {units[unit]}";
    }
}
