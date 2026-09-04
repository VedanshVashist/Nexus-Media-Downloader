using Nexus.Core.Enums;

namespace Nexus.Core.Interfaces;

/// <summary>
/// Applies visual themes and accent overrides at runtime without an app restart.
/// Implemented in the UI layer over WPF ResourceDictionaries.
/// </summary>
public interface IThemeService
{
    /// <summary>The currently applied theme.</summary>
    ThemeType CurrentTheme { get; }

    /// <summary>Raised after a theme change is applied.</summary>
    event EventHandler<ThemeType>? ThemeChanged;

    /// <summary>Swaps the active theme's resource dictionary live.</summary>
    void ApplyTheme(ThemeType theme);

    /// <summary>Overrides the accent color (e.g. "#RRGGBB"); null restores the theme default.</summary>
    void ApplyAccentColor(string? accentColor);
}
