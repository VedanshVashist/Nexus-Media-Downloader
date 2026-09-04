using Microsoft.EntityFrameworkCore;
using Nexus.Core.Models;

namespace Nexus.Infrastructure.Database;

/// <summary>
/// EF Core context for Nexus. Persists only metadata and paths — never media
/// files. Mapping is configured via Fluent API so the Core POCOs stay free of
/// persistence attributes.
/// </summary>
public sealed class NexusDbContext : DbContext
{
    public NexusDbContext(DbContextOptions<NexusDbContext> options) : base(options)
    {
    }

    public DbSet<HistoryEntry> History => Set<HistoryEntry>();
    public DbSet<FavoriteEntry> Favorites => Set<FavoriteEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HistoryEntry>(entity =>
        {
            entity.ToTable("History");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.Channel).HasMaxLength(512);
            entity.Property(e => e.Format).HasMaxLength(32);
            entity.Property(e => e.Quality).HasMaxLength(32);
            entity.HasIndex(e => e.DownloadedAt);
            entity.HasIndex(e => e.IsFavorite);
        });

        modelBuilder.Entity<FavoriteEntry>(entity =>
        {
            entity.ToTable("Favorites");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.Channel).HasMaxLength(512);
            entity.HasIndex(e => e.Url).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
