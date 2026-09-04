namespace Nexus.App.ViewModels;

/// <summary>
/// Sidebar navigation glyphs as Segoe Fluent / MDL2 Assets code points, converted
/// to strings at runtime. Kept as integer code points so the source stays pure
/// ASCII (no private-use glyph characters embedded in .cs files).
/// </summary>
internal static class NavGlyph
{
    private static string From(int codePoint) => char.ConvertFromUtf32(codePoint);

    public static string Home { get; } = From(0xE80F);
    public static string Downloads { get; } = From(0xE896);
    public static string Queue { get; } = From(0xE8FD);
    public static string History { get; } = From(0xE823);
    public static string Favorites { get; } = From(0xE734);
    public static string Settings { get; } = From(0xE713);
    public static string About { get; } = From(0xE946);
}
