using Nexus.Core.Models;

namespace Nexus.Core.DTOs;

/// <summary>
/// Outcome of analyzing a URL. A URL resolves to either a single video or a
/// playlist; exactly one of the payload properties is populated.
/// </summary>
public sealed record UrlAnalysisResult
{
    /// <summary>True when the URL resolved to a playlist.</summary>
    public bool IsPlaylist => Playlist is not null;

    /// <summary>Populated for single-item URLs.</summary>
    public VideoInfo? Video { get; init; }

    /// <summary>Populated for playlist URLs.</summary>
    public PlaylistInfo? Playlist { get; init; }

    public static UrlAnalysisResult ForVideo(VideoInfo video) => new() { Video = video };
    public static UrlAnalysisResult ForPlaylist(PlaylistInfo playlist) => new() { Playlist = playlist };
}
