using Dashboard.Infrastructure.Seeding;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDashboardSeeding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SeedSettings>(
            configuration.GetSection(SeedSettings.SectionName));
        services.AddScoped<DbSeeder>();
        return services;
    }
}
