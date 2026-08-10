using Dashboard.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Persistence;

public class DashboardDbContext : DbContext
{
    public DashboardDbContext(DbContextOptions<DashboardDbContext> options)
        : base(options) { }

    public DbSet<HabitEntry> HabitEntries => Set<HabitEntry>();
    public DbSet<Quote> Quotes => Set<Quote>();

    /// <summary>
    /// Configurations for the <c>research</c> schema live in this assembly too,
    /// but they belong to <see cref="Research.ResearchDbContext"/>. This context
    /// owns the migration history, so it must not see them: scanning the whole
    /// assembly would pull those tables into the model, and the next generated
    /// migration would try to create tables another repository owns.
    /// </summary>
    private const string ForeignSchemaNamespace = "Dashboard.Infrastructure.Research";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(DashboardDbContext).Assembly,
            type => type.Namespace?.StartsWith(ForeignSchemaNamespace, StringComparison.Ordinal) != true);
    }
}
