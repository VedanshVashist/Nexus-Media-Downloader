using Nexus.Core.Models;

namespace Nexus.Core.Interfaces;

/// <summary>Persistence for download history entries.</summary>
public interface IHistoryRepository
{
    Task<IReadOnlyList<HistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<HistoryEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(HistoryEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(HistoryEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Deletes a history entry. Never deletes the underlying media file.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
