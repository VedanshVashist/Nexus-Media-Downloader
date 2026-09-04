using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Nexus.App.Converters;

/// <summary>
/// Loads a local file path or absolute http(s) URL into a cached
/// <see cref="ImageSource"/>. Uses OnLoad caching so the file is not locked, and
/// decodes at a bounded width for memory efficiency. Returns null on any failure
/// so callers can show a placeholder.
/// </summary>
public sealed class PathToImageSourceConverter : IValueConverter
{
    /// <summary>Optional decode width (px). 0 leaves the image at native size.</summary>
    public int DecodeWidth { get; set; } = 320;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var source = value as string;
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        try
        {
            Uri uri;
            if (Uri.TryCreate(source, UriKind.Absolute, out var abs) &&
                (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps))
            {
                uri = abs;
            }
            else
            {
                if (!File.Exists(source))
                {
                    return null;
                }

                uri = new Uri(source, UriKind.Absolute);
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.None;
            if (DecodeWidth > 0)
            {
                bitmap.DecodePixelWidth = DecodeWidth;
            }

            bitmap.UriSource = uri;
            bitmap.EndInit();
            if (bitmap.CanFreeze)
            {
                bitmap.Freeze();
            }

            return bitmap;
        }
        catch
        {
            // Corrupt/unreadable image or network failure: fall back to placeholder.
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// Resolves a stored wallpaper <em>file name</em> to a bounded, cached
/// <see cref="ImageSource"/> for gallery previews. The wallpapers folder root is set
/// once at startup. Only the file-name component is combined, guarding against path
/// traversal. Returns null when the file cannot be resolved or decoded.
/// </summary>
public sealed class WallpaperImageConverter : IValueConverter
{
    /// <summary>Absolute path to the wallpapers folder. Assigned once during startup.</summary>
    public static string? Directory { get; set; }

    /// <summary>Decode width (px) for the preview thumbnail.</summary>
    public int DecodeWidth { get; set; } = 360;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Directory is null || value is not string fileName || string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var path = Path.Combine(Directory, Path.GetFileName(fileName));
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.None;
            if (DecodeWidth > 0)
            {
                bitmap.DecodePixelWidth = DecodeWidth;
            }

            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            if (bitmap.CanFreeze)
            {
                bitmap.Freeze();
            }

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// Multiplies a double by the ConverterParameter (parsed invariantly). Useful for
/// deriving sizes/opacities from a single bound value.
/// </summary>
public sealed class MultiplyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var input = value is double d ? d : 0;
        var factor = 1.0;
        if (parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
        {
            factor = f;
        }

        return input * factor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
