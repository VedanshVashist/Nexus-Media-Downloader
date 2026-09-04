namespace Nexus.Core.Enums;

/// <summary>Distinguishes human-authored subtitles from machine-generated captions.</summary>
public enum SubtitleType
{
    /// <summary>Manually authored subtitles provided by the uploader.</summary>
    Manual = 0,

    /// <summary>Automatically generated captions (e.g. ASR).</summary>
    Automatic = 1
}
