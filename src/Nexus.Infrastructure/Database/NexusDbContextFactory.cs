using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexus.Infrastructure.Settings;

namespace Nexus.Infrastructure.Database;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> can construct the context
/// without running the full application/DI graph. Points at the same LocalAppData
/// database path used at runtime.
/// </summary>
public sealed class NexusDbContextFactory : IDesignTimeDbContextFactory<NexusDbContext>
{
    public NexusDbContext CreateDbContext(string[] args)
    {
        var paths = new AppPaths();
        paths.EnsureCreated();

        var options = new DbContextOptionsBuilder<NexusDbContext>()
            .UseSqlite($"Data Source={paths.DatabasePath}")
            .Options;

        return new NexusDbContext(options);
    }
}
