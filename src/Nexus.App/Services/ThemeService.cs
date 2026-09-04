using System.Windows;
using System.Windows.Media;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;

namespace Nexus.App.Services;

/// <summary>
/// WPF implementation of <see cref="IThemeService"/>. Swaps the active theme
/// ResourceDictionary in <see cref="Application.Resources"/> at runtime; because
/// control styles reference theme colors via <c>DynamicResource</c>, the UI
/// recolors live without a restart. An optional accent override is layered on top
/// of whichever theme is active.
/// </summary>
public sealed class ThemeService : IThemeService
{
    // Marker key every theme dictionary defines, used to locate the current theme.
    private const string ThemeMarkerKey = "Theme.Id";
    private const string AccentKey = "Brush.Accent";
    private const string AccentHoverKey = "Brush.Accent.Hover";
    private const string AccentMutedKey = "Brush.Accent.Muted";
    private const string AccentColorKey = "Color.Accent";

    private readonly IUiDispatcher _dispatcher;
    private string? _accentOverride;

    public ThemeService(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public ThemeType CurrentTheme { get; private set; } = ThemeType.Midnight;

    public event EventHandler<ThemeType>? ThemeChanged;

    public void ApplyTheme(ThemeType theme)
    {
        _dispatcher.Invoke(() =>
        {
            var app = Application.Current;
            if (app is null)
            {
                return;
            }

            var newDict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/Themes/{theme}.xaml", UriKind.Absolute)
            };

            var dictionaries = app.Resources.MergedDictionaries;
            var existing = dictionaries.FirstOrDefault(d => d.Contains(ThemeMarkerKey));
            if (existing is not null)
            {
                var index = dictionaries.IndexOf(existing);
                dictionaries[index] = newDict;
            }
            else
            {
                dictionaries.Add(newDict);
            }

            CurrentTheme = theme;

            // Re-apply any accent override so it survives the theme swap.
            if (!string.IsNullOrWhiteSpace(_accentOverride))
            {
                ApplyAccentInternal(_accentOverride);
            }
        });

        ThemeChanged?.Invoke(this, theme);
    }

    public void ApplyAccentColor(string? accentColor)
    {
        _accentOverride = string.IsNullOrWhiteSpace(accentColor) ? null : accentColor;
        _dispatcher.Invoke(() =>
        {
            if (_accentOverride is null)
            {
                RestoreThemeAccent();
            }
            else
            {
                ApplyAccentInternal(_accentOverride);
            }
        });
    }

    private void ApplyAccentInternal(string accentColor)
    {
        var app = Application.Current;
        if (app is null || !TryParseColor(accentColor, out var color))
        {
            return;
        }

        var accent = new SolidColorBrush(color);
        accent.Freeze();

        var hover = new SolidColorBrush(Adjust(color, 1.14));
        hover.Freeze();

        var muted = new SolidColorBrush(Color.FromArgb(0x33, color.R, color.G, color.B));
        muted.Freeze();

        app.Resources[AccentColorKey] = color;
        app.Resources[AccentKey] = accent;
        app.Resources[AccentHoverKey] = hover;
        app.Resources[AccentMutedKey] = muted;
    }

    private static void RestoreThemeAccent()
    {
        // Removing the app-level overrides lets DynamicResource fall through to the
        // active theme dictionary's own accent definitions.
        var res = Application.Current!.Resources;
        foreach (var key in new[] { AccentColorKey, AccentKey, AccentHoverKey, AccentMutedKey })
        {
            if (res.Contains(key))
            {
                res.Remove(key);
            }
        }
    }

    private static Color Adjust(Color color, double factor)
    {
        byte Clamp(double v) => (byte)Math.Clamp(v, 0, 255);
        return Color.FromArgb(color.A, Clamp(color.R * factor), Clamp(color.G * factor), Clamp(color.B * factor));
    }

    private static bool TryParseColor(string value, out Color color)
    {
        try
        {
            var parsed = ColorConverter.ConvertFromString(value);
            if (parsed is Color c)
            {
                color = c;
                return true;
            }
        }
        catch (FormatException)
        {
            // Ignore malformed accent strings and keep the theme default.
        }

        color = default;
        return false;
    }
}
