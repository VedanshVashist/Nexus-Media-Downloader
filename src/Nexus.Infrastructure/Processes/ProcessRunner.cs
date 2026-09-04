using System.Diagnostics;
using System.Text;

namespace Nexus.Infrastructure.Processes;

/// <summary>
/// Runs external executables safely. Every argument is passed via
/// <see cref="ProcessStartInfo.ArgumentList"/> — the tool never sees a shell, so
/// user-supplied URLs, paths, and arguments cannot be interpreted as commands.
/// </summary>
/// <remarks>
/// Two modes are provided: <see cref="RunAsync"/> buffers output and returns it,
/// while <see cref="StreamAsync"/> invokes a callback per stdout/stderr line for
/// progress parsing during long-running downloads.
/// </remarks>
public sealed class ProcessRunner
{
    /// <summary>
    /// Runs <paramref name="executablePath"/> with the given arguments to completion,
    /// capturing stdout/stderr.
    /// </summary>
    public async Task<ProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        using var process = CreateProcess(executablePath, arguments, workingDirectory);

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdout.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stderr.AppendLine(e.Data);
            }
        };

        Start(process);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout.ToString(),
            StandardError = stderr.ToString()
        };
    }

    /// <summary>
    /// Runs a process and invokes <paramref name="onOutputLine"/> /
    /// <paramref name="onErrorLine"/> for each line as it arrives. Returns the exit
    /// code and captured stderr (for error reporting). stdout is not buffered.
    /// </summary>
    public async Task<ProcessResult> StreamAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        Action<string>? onOutputLine,
        Action<string>? onErrorLine = null,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        using var process = CreateProcess(executablePath, arguments, workingDirectory);

        // stderr is kept (tail) for diagnostics; stdout is streamed only.
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutputLine?.Invoke(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onErrorLine?.Invoke(e.Data);
                stderr.AppendLine(e.Data);
            }
        };

        Start(process);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = string.Empty,
            StandardError = stderr.ToString()
        };
    }

    private static Process CreateProcess(string executablePath, IReadOnlyList<string> arguments, string? workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException("Executable path must be provided.", nameof(executablePath));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        // Critical: each argument added individually. No shell, no concatenation.
        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        return new Process { StartInfo = startInfo, EnableRaisingEvents = true };
    }

    private static void Start(Process process)
    {
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort cleanup; the process may have already exited.
        }
    }
}
