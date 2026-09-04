using Nexus.Core.Models;

namespace Nexus.Core.Interfaces;

/// <summary>Persistence for favorite entries.</summary>
public interface IFavoritesRepository
{
    Task<IReadOnlyList<FavoriteEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string url, CancellationToken cancellationToken = default);
    Task AddAsync(FavoriteEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
