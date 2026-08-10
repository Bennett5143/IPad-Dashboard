using Dashboard.Domain.Research;
using Dashboard.Infrastructure.Persistence;
using Dashboard.Infrastructure.Research;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' missing.");

        services.AddDbContextFactory<DashboardDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()));

        // The research schema: same database, different owner. Its own context
        // so the one that owns migrations never learns these tables exist.
        services.AddDbContextFactory<ResearchDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IResearchRepository, ResearchRepository>();

        return services;
    }
}
