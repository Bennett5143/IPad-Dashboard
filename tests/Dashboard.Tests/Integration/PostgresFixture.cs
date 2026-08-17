using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace Dashboard.Tests.Integration;

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}

/// <summary>
/// One PostGIS container for the whole collection, running the same image as
/// production. All migrations are applied once on startup — a second migration
/// exercise on top of the CI smoke job. Test classes call <see cref="ResetAsync"/>
/// between each other (via IAsyncLifetime) to start from empty tables.
/// Stays inert unless RUN_INTEGRATION_TESTS=1, so a plain <c>dotnet test</c>
/// never touches Docker even though xUnit may instantiate the fixture.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private DbContextOptions<DashboardDbContext>? _options;

    internal IDbContextFactory<DashboardDbContext> Factory =>
        new FixedOptionsFactory(_options
            ?? throw new InvalidOperationException(
                "Fixture is inert — integration tests must be gated with [IntegrationFact]."));

    public async Task InitializeAsync()
    {
        if (!IntegrationFactAttribute.Enabled)
        {
            return;
        }

        _container = new PostgreSqlBuilder("imresamu/postgis:16-3.5").Build();
        await _container.StartAsync();

        _options = new DbContextOptionsBuilder<DashboardDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql => npgsql.UseNetTopologySuite())
            .Options;

        await using var db = new DashboardDbContext(_options);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    internal async Task ResetAsync()
    {
        await using var db = Factory.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE "RunActivities", "RunPlaces", "StravaTokens", "SyncStates",
                "WhoopDailyMetrics", "WhoopWorkouts", "WhoopTokens", "WhoopProcessedWorkouts",
                "HabitEntries", "EmomWorkout", "EmomSegment", "Quotes"
            RESTART IDENTITY CASCADE
            """);
    }

    private sealed class FixedOptionsFactory(DbContextOptions<DashboardDbContext> options)
        : IDbContextFactory<DashboardDbContext>
    {
        public DashboardDbContext CreateDbContext() => new(options);
    }
}
