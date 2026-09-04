using Microsoft.EntityFrameworkCore;
using Nexus.Core.Exceptions;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.Infrastructure.Database;

/// <summary>EF Core-backed history persistence. Deleting entries never touches media files.</summary>
public sealed class HistoryRepository : IHistoryRepository
{
    private readonly IDbContextFactory<NexusDbContext> _contextFactory;

    public HistoryRepository(IDbContextFactory<NexusDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<HistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.History
            .AsNoTracking()
            .OrderByDescending(h => h.DownloadedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<HistoryEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.History.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(HistoryEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        try
        {
            await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.History.Add(entry);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            throw new DatabaseException("Failed to add history entry.", innerException: ex);
        }
    }

    public async Task UpdateAsync(HistoryEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        try
        {
            await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.History.Update(entry);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            throw new DatabaseException("Failed to update history entry.", innerException: ex);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.History.Where(h => h.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.History.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
