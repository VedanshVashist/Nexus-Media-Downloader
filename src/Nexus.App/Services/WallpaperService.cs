using System.IO;
using Microsoft.Extensions.Logging;
using Nexus.Core.Utilities;
using Nexus.Infrastructure.Settings;

namespace Nexus.App.Services;

/// <summary>
/// Manages user wallpaper images: importing a chosen file into the app's
/// Wallpapers folder (so the source can move/delete freely), listing stored
/// wallpapers, and resolving a stored file name back to an absolute path.
/// </summary>
public interface IWallpaperService
{
    IReadOnlyList<string> ListWallpapers();
    Task<string?> ImportAsync(string sourcePath, CancellationToken cancellationToken = default);
    string? ResolvePath(string? fileName);
    void Delete(string fileName);
}

/// <inheritdoc />
public sealed class WallpaperService : IWallpaperService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif" };

    private readonly AppPaths _paths;
    private readonly ILogger<WallpaperService> _logger;

    public WallpaperService(AppPaths paths, ILogger<WallpaperService> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public IReadOnlyList<string> ListWallpapers()
    {
        try
        {
            if (!Directory.Exists(_paths.WallpapersDirectory))
            {
                return [];
            }

            return Directory.EnumerateFiles(_paths.WallpapersDirectory)
                .Where(f => AllowedExtensions.Contains(Path.GetExtension(f)))
                .Select(Path.GetFileName)
                .Where(f => f is not null)
                .Select(f => f!)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to list wallpapers.");
            return [];
        }
    }

    public async Task<string?> ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return null;
        }

        var extension = Path.GetExtension(sourcePath);
        if (!AllowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Rejected wallpaper with unsupported extension {Extension}.", extension);
            return null;
        }

        Directory.CreateDirectory(_paths.WallpapersDirectory);

        var baseName = FilenameSanitizer.SanitizeComponent(Path.GetFileNameWithoutExtension(sourcePath));
        var fileName = baseName + extension;
        var target = Path.Combine(_paths.WallpapersDirectory, fileName);

        // Avoid clobbering an existing wallpaper of the same name.
        var counter = 1;
        while (File.Exists(target))
        {
            fileName = $"{baseName} ({counter}){extension}";
            target = Path.Combine(_paths.WallpapersDirectory, fileName);
            counter++;
        }

        await using (var source = File.OpenRead(sourcePath))
        await using (var dest = File.Create(target))
        {
            await source.CopyToAsync(dest, cancellationToken).ConfigureAwait(false);
        }

        return fileName;
    }

    public string? ResolvePath(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        // Guard against path traversal: only ever resolve within the wallpapers folder.
        var candidate = Path.Combine(_paths.WallpapersDirectory, Path.GetFileName(fileName));
        return File.Exists(candidate) ? candidate : null;
    }

    public void Delete(string fileName)
    {
        var path = ResolvePath(fileName);
        if (path is null)
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to delete wallpaper {File}.", fileName);
        }
    }
}
