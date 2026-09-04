namespace Nexus.Infrastructure.Processes;

/// <summary>Captured outcome of running an external process to completion.</summary>
public sealed record ProcessResult
{
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = string.Empty;
    public string StandardError { get; init; } = string.Empty;
    public bool Success => ExitCode == 0;
}
