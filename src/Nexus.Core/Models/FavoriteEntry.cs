namespace Nexus.Core.Models;

/// <summary>
/// A persisted favorite: a URL (and its captured metadata) the user wants to keep
/// for quick re-download. Distinct from history so a favorite survives history
/// pruning.
/// </summary>
public sealed class FavoriteEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ThumbnailPath { get; set; }

    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}
