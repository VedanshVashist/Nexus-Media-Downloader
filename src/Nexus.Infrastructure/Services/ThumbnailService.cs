using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Nexus.Core.Interfaces;
using Nexus.Core.Utilities;
using Nexus.Infrastructure.Settings;

namespace Nexus.Infrastructure.Services;

/// <summary>
/// Downloads and caches thumbnail images. Cached files are keyed by a hash of the
/// URL so repeated previews reuse the same file. Saving to a user directory uses
/// a sanitized base name.
/// </summary>
public sealed class ThumbnailService : IThumbnailService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppPaths _paths;
    private readonly ILogger<ThumbnailService> _logger;

    public ThumbnailService(IHttpClientFactory httpClientFactory, AppPaths paths, ILogger<ThumbnailService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _paths = paths;
        _logger = logger;
    }

    public async Task<string?> GetCachedThumbnailAsync(string? thumbnailUrl, string cacheKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            return null;
        }

        Directory.CreateDirectory(_paths.ThumbnailCacheDirectory);

        var ext = GuessExtension(thumbnailUrl);
        var fileName = $"{Hash(cacheKey)}{ext}";
        var cachePath = Path.Combine(_paths.ThumbnailCacheDirectory, fileName);

        if (File.Exists(cachePath))
        {
            return cachePath;
        }

        try
        {
            await DownloadToAsync(thumbnailUrl, cachePath, cancellationToken).ConfigureAwait(false);
            return cachePath;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to cache thumbnail.");
            return null;
        }
    }

    public async Task<string> SaveThumbnailAsync(string thumbnailUrl, string directory, string baseName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            throw new ArgumentException("Thumbnail URL is required.", nameof(thumbnailUrl));
        }

        Directory.CreateDirectory(directory);

        var ext = GuessExtension(thumbnailUrl);
        var safeName = FilenameSanitizer.SanitizeComponent(baseName) + ext;
        var target = Path.Combine(directory, safeName);

        await DownloadToAsync(thumbnailUrl, target, cancellationToken).ConfigureAwait(false);
        return target;
    }

    private async Task DownloadToAsync(string url, string targetPath, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("downloads");
        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var tempPath = targetPath + ".tmp";
        await using (var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        await using (var fileStream = File.Create(tempPath))
        {
            await httpStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, targetPath, overwrite: true);
    }

    private static string GuessExtension(string url)
    {
        var ext = Path.GetExtension(new Uri(url, UriKind.Absolute).AbsolutePath);
        return string.IsNullOrWhiteSpace(ext) || ext.Length > 5 ? ".jpg" : ext;
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
