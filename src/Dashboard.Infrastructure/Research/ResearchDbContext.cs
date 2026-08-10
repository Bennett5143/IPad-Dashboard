using Dashboard.Domain.Research;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Research;

/// <summary>
/// The <c>research</c> schema, read-only.
///
/// Two guards keep this app from ever owning these tables:
/// 1. every entity is mapped with <c>ExcludeFromMigrations()</c>, so the
///    migration generator ignores them even if it is pointed at this context;
/// 2. they live here and not in <see cref="Persistence.DashboardDbContext"/> —
///    the context that does have a migration history never learns they exist.
///
/// The tables are created and versioned by the research repository
/// (<c>Bennett5143/research</c>) through its own numbered SQL migrations. One
/// schema, one writer; this side only reads.
/// </summary>
public sealed class ResearchDbContext : DbContext
{
    public const string SchemaName = "research";

    public ResearchDbContext(DbContextOptions<ResearchDbContext> options)
        : base(options)
    {
        // Nothing here is ever saved, so tracking would only cost memory —
        // and a tracked entity is the first step towards an accidental write.
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<FootballNewsItem> FootballNews => Set<FootballNewsItem>();
    public DbSet<MarketQuote> MarketQuotes => Set<MarketQuote>();
    public DbSet<MarketDriver> MarketDrivers => Set<MarketDriver>();
    public DbSet<MarketEvent> MarketEvents => Set<MarketEvent>();
    public DbSet<ElliottWaveView> ElliottWaveViews => Set<ElliottWaveView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ResearchDbContext).Assembly,
            type => type.Namespace == typeof(ResearchDbContext).Namespace + ".Configurations");
    }

    /// <summary>
    /// Saving is not part of this context's job. Overridden to fail loudly
    /// rather than to write into a schema with a different owner.
    /// </summary>
    public override int SaveChanges() => throw new NotSupportedException(
        "The research schema is written by the research repository, not by this application.");

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "The research schema is written by the research repository, not by this application.");
}
