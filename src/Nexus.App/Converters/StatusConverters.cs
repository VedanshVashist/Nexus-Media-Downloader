using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Nexus.Core.Enums;

namespace Nexus.App.Converters;

/// <summary>
/// Maps a <see cref="DownloadStatus"/> to a semantic theme brush by resource key,
/// so status pills recolor with the active theme. Falls back to the muted text
/// brush when a key is missing.
/// </summary>
public sealed class DownloadStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value is DownloadStatus status
            ? status switch
            {
                DownloadStatus.Completed => "Brush.Success",
                DownloadStatus.Failed => "Brush.Error",
                DownloadStatus.Cancelled => "Brush.Warning",
                DownloadStatus.Downloading => "Brush.Accent",
                DownloadStatus.Processing => "Brush.Info",
                DownloadStatus.Paused => "Brush.Warning",
                DownloadStatus.Queued => "Brush.Text.Secondary",
                _ => "Brush.Text.Muted"
            }
            : "Brush.Text.Muted";

        return Application.Current?.TryFindResource(key) as Brush
            ?? Application.Current?.TryFindResource("Brush.Text.Muted") as Brush
            ?? Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Maps a <see cref="DownloadStatus"/> to a friendly label.</summary>
public sealed class DownloadStatusToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DownloadStatus status
            ? status switch
            {
                DownloadStatus.Created => "Ready",
                DownloadStatus.Queued => "Queued",
                DownloadStatus.Downloading => "Downloading",
                DownloadStatus.Processing => "Processing",
                DownloadStatus.Paused => "Paused",
                DownloadStatus.Completed => "Completed",
                DownloadStatus.Failed => "Failed",
                DownloadStatus.Cancelled => "Cancelled",
                _ => status.ToString()
            }
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Shared glyph code-point constants (Segoe MDL2 Assets / Segoe Fluent Icons).</summary>
internal static class Glyphs
{
    // Actions / status
    public const int Download = 0xE896;
    public const int History = 0xE823;
    public const int Sync = 0xE895;
    public const int Pause = 0xE769;
    public const int Play = 0xE768;
    public const int CheckMark = 0xE73E;
    public const int Error = 0xE783;
    public const int Cancel = 0xE711;
    public const int Refresh = 0xE72C;
    public const int Unknown = 0xE9CE;

    // Media types
    public const int Video = 0xE714;
    public const int Music = 0xE8D6;
    public const int Photo = 0xEB9F;
    public const int Message = 0xE90A;
    public const int Document = 0xE8A5;

    public static string Text(int codePoint) => char.ConvertFromUtf32(codePoint);
}

/// <summary>
/// Maps a <see cref="DownloadStatus"/> to a Segoe MDL2 / Fluent glyph so status is
/// legible at a glance on download cards.
/// </summary>
public sealed class DownloadStatusToGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Glyphs.Text(value is DownloadStatus status
            ? status switch
            {
                DownloadStatus.Created => Glyphs.Download,
                DownloadStatus.Queued => Glyphs.History,
                DownloadStatus.Downloading => Glyphs.Download,
                DownloadStatus.Processing => Glyphs.Sync,
                DownloadStatus.Paused => Glyphs.Pause,
                DownloadStatus.Completed => Glyphs.CheckMark,
                DownloadStatus.Failed => Glyphs.Error,
                DownloadStatus.Cancelled => Glyphs.Cancel,
                _ => Glyphs.Unknown
            }
            : Glyphs.Unknown);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// Maps a <see cref="DownloadType"/> to a Segoe MDL2 / Fluent glyph for compact
/// type badges on cards and history rows.
/// </summary>
public sealed class DownloadTypeToGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Glyphs.Text(value is DownloadType type
            ? type switch
            {
                DownloadType.Video => Glyphs.Video,
                DownloadType.Audio => Glyphs.Music,
                DownloadType.Thumbnail => Glyphs.Photo,
                DownloadType.Subtitle => Glyphs.Message,
                DownloadType.Metadata => Glyphs.Document,
                _ => Glyphs.Document
            }
            : Glyphs.Document);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
