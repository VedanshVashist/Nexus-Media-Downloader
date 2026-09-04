using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.Infrastructure.Settings;

/// <summary>
/// JSON-backed <see cref="ISettingsService"/>. Loads once into memory, writes
/// atomically (temp file + move) to avoid corrupting settings on a crash, and
/// raises <see cref="SettingsChanged"/> so services react to changes live.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AppPaths _paths;
    private readonly ILogger<SettingsService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private AppSettings _current = new();

    public SettingsService(AppPaths paths, ILogger<SettingsService> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public AppSettings Current => _current;

    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _paths.EnsureCreated();

            if (!File.Exists(_paths.SettingsPath))
            {
                _current = CreateDefaults();
                await WriteAsync(_current, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Created default settings at {Path}", _paths.SettingsPath);
                return;
            }

            await using var stream = File.OpenRead(_paths.SettingsPath);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            _current = loaded ?? CreateDefaults();
            _logger.LogInformation("Loaded settings (schema v{Version})", _current.SchemaVersion);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Corrupt or unreadable settings must not crash startup: fall back to defaults.
            _logger.LogError(ex, "Failed to read settings; falling back to defaults.");
            _current = CreateDefaults();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteAsync(settings, cancellationToken).ConfigureAwait(false);
            _current = settings;
        }
        finally
        {
            _gate.Release();
        }

        SettingsChanged?.Invoke(this, settings);
        _logger.LogInformation("Settings saved.");
    }

    private async Task WriteAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        _paths.EnsureCreated();

        // Atomic write: serialize to a temp file, then move over the target.
        var tempPath = _paths.SettingsPath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        File.Move(tempPath, _paths.SettingsPath, overwrite: true);
    }

    private static AppSettings CreateDefaults() => new()
    {
        Downloads = new DownloadSettings
        {
            DefaultDownloadDirectory = AppPaths.DefaultDownloadDirectory()
        }
    };
}
