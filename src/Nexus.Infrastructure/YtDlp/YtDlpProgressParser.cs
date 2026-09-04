using System.Globalization;
using Nexus.Core.DTOs;
using Nexus.Core.Enums;

namespace Nexus.Infrastructure.YtDlp;

/// <summary>
/// Parses the machine-readable progress lines emitted by the custom
/// <c>--progress-template</c> configured in <see cref="YtDlpArgumentBuilder"/>.
/// Lines look like:
/// <c>NEXUS_PROGRESS downloading 1234 5000 NA 250000 12</c>
/// (status, downloaded, total, total_estimate, speed, eta). "NA" marks unknown
/// values (yt-dlp prints "NA" for missing numeric fields).
/// </summary>
public static class YtDlpProgressParser
{
    public const string Marker = "NEXUS_PROGRESS";

    /// <summary>
    /// Attempts to parse a single output line into a <see cref="DownloadProgress"/>.
    /// Returns null for non-progress lines.
    /// </summary>
    public static DownloadProgress? TryParse(string line, Guid taskId)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var markerIndex = line.IndexOf(Marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        var payload = line[(markerIndex + Marker.Length)..].Trim();
        var parts = payload.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 6)
        {
            return null;
        }

        var status = MapStatus(parts[0]);
        var downloaded = ParseLong(parts[1]);
        var total = ParseLong(parts[2]);
        var totalEstimate = ParseLong(parts[3]);
        var speed = ParseDouble(parts[4]);
        var etaSeconds = ParseDouble(parts[5]);

        var effectiveTotal = total ?? totalEstimate ?? 0;
        var percent = effectiveTotal > 0 && downloaded is not null
            ? Math.Clamp(downloaded.Value / (double)effectiveTotal * 100.0, 0, 100)
            : 0;

        return new DownloadProgress
        {
            TaskId = taskId,
            Status = status,
            Percent = percent,
            DownloadedBytes = downloaded ?? 0,
            TotalBytes = effectiveTotal,
            SpeedBytesPerSecond = speed ?? 0,
            Eta = etaSeconds is > 0 ? TimeSpan.FromSeconds(etaSeconds.Value) : null
        };
    }

    private static DownloadStatus MapStatus(string raw) => raw.ToLowerInvariant() switch
    {
        "downloading" => DownloadStatus.Downloading,
        "finished" => DownloadStatus.Processing,
        "error" => DownloadStatus.Failed,
        _ => DownloadStatus.Downloading
    };

    private static long? ParseLong(string value)
    {
        if (string.IsNullOrEmpty(value) || value is "NA" or "None")
        {
            return null;
        }

        // yt-dlp may emit floats like "1234.0"; parse tolerantly.
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            return (long)d;
        }

        return null;
    }

    private static double? ParseDouble(string value)
    {
        if (string.IsNullOrEmpty(value) || value is "NA" or "None")
        {
            return null;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}
