using Dashboard.Web.Components;
using Dashboard.Web.Infrastructure;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starte Dashboard.Web");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // DB
    builder.Services.AddInfrastructure(builder.Configuration);

    // Health-Check
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' fehlt.");

    builder.Services
        .AddHealthChecks()
        .AddNpgSql(
            connectionString: connectionString,
            name: "postgres",
            tags: ["db", "ready"]);

    // DbSeeder
    builder.Services.AddDashboardSeeding(builder.Configuration);

    // Time
    builder.Services.AddSingleton<IClock, SystemClock>();

    var app = builder.Build();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        // Keine Checks ausführen, nur prüfen ob die App antwortet
        Predicate = _ => false,
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        // Nur Checks ausführen, die mit "ready" getaggt sind
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    });

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    await app.SeedDatabaseAsync();

    app.Run();


}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Dashboard.Web ist beim Starten abgestürzt");
}
finally
{
    Log.CloseAndFlush();
}
