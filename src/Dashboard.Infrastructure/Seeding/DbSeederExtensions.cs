using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure.Seeding;

public static class DbSeederExtensions
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }
}