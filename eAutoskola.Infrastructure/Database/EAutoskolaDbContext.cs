using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Infrastructure.Database;

/// <summary>
/// Placeholder context created during the solution-skeleton phase. DbSets, entity
/// configurations and the initial migration are added once the ERD (docs/ phase 3) is
/// approved - see CLAUDE.md working conventions.
/// </summary>
public class EAutoskolaDbContext : DbContext
{
    public EAutoskolaDbContext(DbContextOptions<EAutoskolaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations are applied here via ApplyConfigurationsFromAssembly once the
        // ERD is in place.
    }
}
