namespace Nexus.Core.Interfaces;

/// <summary>
/// Retrieves and caches thumbnail images for preview and for saving alongside
/// downloads. Network work is async and cancellable.
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// Ensures a thumbnail for the given URL is cached locally and returns its path.
    /// Returns null when the URL is empty or the fetch fails.
    /// </summary>
    Task<string?> GetCachedThumbnailAsync(string? thumbnailUrl, string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the thumbnail to a chosen directory using a sanitized file name
    /// derived from <paramref name="baseName"/>. Returns the saved path.
    /// </summary>
    Task<string> SaveThumbnailAsync(string thumbnailUrl, string directory, string baseName, CancellationToken cancellationToken = default);
}
