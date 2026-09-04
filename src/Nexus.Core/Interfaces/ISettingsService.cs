using Nexus.Core.Models;

namespace Nexus.Core.Interfaces;

/// <summary>
/// Loads, persists, and broadcasts changes to <see cref="AppSettings"/>. The
/// current settings are cached in memory; <see cref="SaveAsync"/> writes them
/// atomically to disk.
/// </summary>
public interface ISettingsService
{
    /// <summary>The current, in-memory settings. Never null after initialization.</summary>
    AppSettings Current { get; }

    /// <summary>Raised after settings are saved, carrying the new snapshot.</summary>
    event EventHandler<AppSettings>? SettingsChanged;

    /// <summary>Loads settings from disk, creating defaults on first run.</summary>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the supplied settings atomically and updates <see cref="Current"/>.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
