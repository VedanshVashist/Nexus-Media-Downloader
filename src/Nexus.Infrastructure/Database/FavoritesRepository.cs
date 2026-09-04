using Microsoft.EntityFrameworkCore;
using Nexus.Core.Exceptions;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.Infrastructure.Database;

/// <summary>EF Core-backed favorites persistence. URLs are unique.</summary>
public sealed class FavoritesRepository : IFavoritesRepository
{
    private readonly IDbContextFactory<NexusDbContext> _contextFactory;

    public FavoritesRepository(IDbContextFactory<NexusDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<FavoriteEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Favorites
            .AsNoTracking()
            .OrderByDescending(f => f.AddedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string url, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Favorites.AnyAsync(f => f.Url == url, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(FavoriteEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        try
        {
            await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            // Idempotent on URL: skip if already present.
            if (await db.Favorites.AnyAsync(f => f.Url == entry.Url, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            db.Favorites.Add(entry);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            throw new DatabaseException("Failed to add favorite.", innerException: ex);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.Favorites.Where(f => f.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
