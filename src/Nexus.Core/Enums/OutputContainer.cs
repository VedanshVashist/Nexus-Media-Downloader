namespace Nexus.Core.Enums;

/// <summary>Target output container/format presets offered in the UI.</summary>
public enum OutputContainer
{
    /// <summary>Keep whatever yt-dlp/FFmpeg produces without forcing a container.</summary>
    Auto = 0,
    Mp4 = 1,
    Mkv = 2,
    Webm = 3,
    Mp3 = 4,
    M4a = 5,
    Opus = 6,
    Flac = 7,
    Wav = 8,

    /// <summary>User supplied a custom container/extension.</summary>
    Custom = 99
}
