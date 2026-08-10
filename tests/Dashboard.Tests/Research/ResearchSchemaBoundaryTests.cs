using Dashboard.Domain.Research;
using Dashboard.Infrastructure.Persistence;
using Dashboard.Infrastructure.Research;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Dashboard.Tests.Research;

/// <summary>
/// The boundary this application must not cross.
///
/// The `research` schema is written and versioned by another repository. These
/// tests are the guard rail: if a future model change starts pulling those
/// tables into this application's migration history, they fail here rather than
/// in production, where the failure mode is a migration dropping a table whose
/// contents nobody else has.
/// </summary>
public class ResearchSchemaBoundaryTests
{
    private static DashboardDbContext DashboardContext()
    {
        var options = new DbContextOptionsBuilder<DashboardDbContext>()
            .UseNpgsql("Host=localhost;Database=none;Username=none",
                npgsql => npgsql.UseNetTopologySuite())
            .Options;
        return new DashboardDbContext(options);
    }

    private static ResearchDbContext ResearchContext()
    {
        var options = new DbContextOptionsBuilder<ResearchDbContext>()
            .UseNpgsql("Host=localhost;Database=none;Username=none")
            .Options;
        return new ResearchDbContext(options);
    }

    [Fact]
    public void DashboardModelKnowsNothingAboutTheResearchSchema()
    {
        using var db = DashboardContext();

        var schemas = db.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .Select(entity => entity.GetSchema())
            .Distinct()
            .ToList();

        Assert.DoesNotContain(ResearchDbContext.SchemaName, schemas);
    }

    [Fact]
    public void EveryResearchEntityIsExcludedFromMigrations()
    {
        using var db = ResearchContext();

        foreach (var entity in db.GetService<IDesignTimeModel>().Model.GetEntityTypes())
        {
            Assert.True(
                entity.IsTableExcludedFromMigrations(),
                $"{entity.DisplayName()} is not excluded from migrations — a generated " +
                "migration could create, alter or drop a table this application does not own.");
        }
    }

    [Fact]
    public void NoGeneratedMigrationOperationTouchesTheResearchSchema()
    {
        // The real test: ask EF what it WOULD do to reach the current model,
        // and assert that none of it reaches into the foreign schema.
        using var db = ResearchContext();

        var differ = db.GetService<IMigrationsModelDiffer>();
        var operations = differ.GetDifferences(null, db.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        Assert.Empty(operations);
    }

    [Fact]
    public void TheDashboardMigrationHistoryStaysOutOfTheResearchSchema()
    {
        using var db = DashboardContext();

        var differ = db.GetService<IMigrationsModelDiffer>();
        var operations = differ.GetDifferences(null, db.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        var offending = operations
            .OfType<MigrationOperation>()
            .Where(MentionsResearchSchema)
            .ToList();

        Assert.Empty(offending);
    }

    private static bool MentionsResearchSchema(MigrationOperation operation) => operation switch
    {
        CreateTableOperation create => create.Schema == ResearchDbContext.SchemaName,
        DropTableOperation drop => drop.Schema == ResearchDbContext.SchemaName,
        AlterTableOperation alter => alter.Schema == ResearchDbContext.SchemaName,
        RenameTableOperation rename => rename.Schema == ResearchDbContext.SchemaName,
        AddColumnOperation add => add.Schema == ResearchDbContext.SchemaName,
        DropColumnOperation dropColumn => dropColumn.Schema == ResearchDbContext.SchemaName,
        AlterColumnOperation alterColumn => alterColumn.Schema == ResearchDbContext.SchemaName,
        CreateIndexOperation index => index.Schema == ResearchDbContext.SchemaName,
        EnsureSchemaOperation schema => schema.Name == ResearchDbContext.SchemaName,
        DropSchemaOperation dropSchema => dropSchema.Name == ResearchDbContext.SchemaName,
        _ => false,
    };

    [Fact]
    public void TheResearchContextRefusesToSave()
    {
        using var db = ResearchContext();

        Assert.Throws<NotSupportedException>(() => db.SaveChanges());
    }

    [Fact]
    public void TheRepositoryInterfaceHasNoWriteMethod()
    {
        // "Read-only by convention" is not read-only. The interface must be
        // unable to express a write at all.
        var forbidden = new[] { "Add", "Save", "Update", "Delete", "Remove", "Insert", "Write" };

        var offending = typeof(IResearchRepository).GetMethods()
            .Where(method => forbidden.Any(word =>
                method.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Select(method => method.Name)
            .ToList();

        Assert.Empty(offending);
    }
}
