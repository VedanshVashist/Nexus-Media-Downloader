using System.Globalization;
using System.Windows.Data;

namespace Nexus.App.Converters;

/// <summary>Formats a byte count as a human-readable size (e.g. "12.3 MB").</summary>
public sealed class BytesToHumanConverter : IValueConverter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var bytes = ToDouble(value);
        if (bytes <= 0)
        {
            return "—";
        }

        var unit = 0;
        while (bytes >= 1024 && unit < Units.Length - 1)
        {
            bytes /= 1024;
            unit++;
        }

        var format = unit == 0 ? "0" : "0.#";
        return string.Create(CultureInfo.InvariantCulture, $"{bytes.ToString(format, CultureInfo.InvariantCulture)} {Units[unit]}");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;

    internal static double ToDouble(object? value) => value switch
    {
        long l => l,
        int i => i,
        double d => d,
        _ => 0
    };
}

/// <summary>Formats a bytes-per-second speed (e.g. "1.4 MB/s"); blank when idle.</summary>
public sealed class SpeedToHumanConverter : IValueConverter
{
    private static readonly BytesToHumanConverter Bytes = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var speed = BytesToHumanConverter.ToDouble(value);
        if (speed <= 0)
        {
            return string.Empty;
        }

        return $"{Bytes.Convert(speed, typeof(string), null, culture)}/s";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Formats a nullable <see cref="TimeSpan"/> ETA (e.g. "2m 05s", "45s").</summary>
public sealed class EtaToHumanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TimeSpan span || span <= TimeSpan.Zero)
        {
            return string.Empty;
        }

        if (span.TotalHours >= 1)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{(int)span.TotalHours}h {span.Minutes:00}m");
        }

        if (span.TotalMinutes >= 1)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{span.Minutes}m {span.Seconds:00}s");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{span.Seconds}s");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Formats a <see cref="TimeSpan"/> duration as [h:]mm:ss.</summary>
public sealed class DurationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TimeSpan span || span <= TimeSpan.Zero)
        {
            return string.Empty;
        }

        return span.TotalHours >= 1
            ? span.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : span.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// Formats a <see cref="TimeSpan"/> position as a timestamp ([h:]mm:ss). Unlike
/// <see cref="DurationConverter"/>, a zero position renders as "0:00" rather than
/// blank — a chapter can legitimately start at the very beginning of a video.
/// </summary>
public sealed class TimestampConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TimeSpan span || span < TimeSpan.Zero)
        {
            return string.Empty;
        }

        return span.TotalHours >= 1
            ? span.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : span.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Converts a <see cref="DateTimeOffset"/> (UTC) to a friendly local date/time.</summary>
public sealed class LocalDateTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            DateTimeOffset dto => dto.ToLocalTime().LocalDateTime.ToString("g", CultureInfo.CurrentCulture),
            DateTime dt => dt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
            _ => string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Formats a large count compactly (e.g. 1.5M, 12.3K views).</summary>
public sealed class CompactNumberConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var number = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        return number switch
        {
            >= 1_000_000_000 => Fmt(number / 1_000_000_000, "B"),
            >= 1_000_000 => Fmt(number / 1_000_000, "M"),
            >= 1_000 => Fmt(number / 1_000, "K"),
            _ => number.ToString("0", CultureInfo.InvariantCulture)
        };

        static string Fmt(double v, string suffix)
            => string.Create(CultureInfo.InvariantCulture, $"{v.ToString("0.#", CultureInfo.InvariantCulture)}{suffix}");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
