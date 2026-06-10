using System.Security.Cryptography;
using System.Text;

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

    // Quotes
    builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
    builder.Services.AddScoped<DailyQuoteService>();

    // Weather
    var weatherOptions = builder.Configuration
        .GetSection(WeatherOptions.SectionName)
        .Get<WeatherOptions>() ?? new WeatherOptions();

    builder.Services.Configure<WeatherOptions>(
        builder.Configuration.GetSection(WeatherOptions.SectionName));
    builder.Services.AddSingleton<WeatherState>();
    builder.Services.AddHttpClient<IWeatherProvider, OpenWeatherMapClient>(http =>
    {
        http.BaseAddress = new Uri(weatherOptions.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(10);
    });
    builder.Services.AddHostedService<WeatherRefreshService>();

    // Football
    var footballOptions = builder.Configuration
        .GetSection(FootballOptions.SectionName)
        .Get<FootballOptions>() ?? new FootballOptions();

    builder.Services.Configure<FootballOptions>(
        builder.Configuration.GetSection(FootballOptions.SectionName));
    builder.Services.AddSingleton<FootballState>();
    builder.Services.AddHttpClient<IFootballProvider, FootballDataClient>(http =>
    {
        http.BaseAddress = new Uri(footballOptions.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(10);
        if (!string.IsNullOrWhiteSpace(footballOptions.ApiKey))
        {
            http.DefaultRequestHeaders.Add("X-Auth-Token", footballOptions.ApiKey);
        }
    });
    builder.Services.AddHostedService<FootballRefreshService>();

    // HVV
    var hvvOptions = builder.Configuration
        .GetSection(HvvOptions.SectionName)
        .Get<HvvOptions>() ?? new HvvOptions();

    builder.Services.Configure<HvvOptions>(
        builder.Configuration.GetSection(HvvOptions.SectionName));
    builder.Services.AddSingleton<HvvState>();
    builder.Services.AddHttpClient<IHvvProvider, HvvDepartureClient>(http =>
    {
        http.Timeout = TimeSpan.FromSeconds(10);
        if (!string.IsNullOrWhiteSpace(hvvOptions.UserAgent))
        {
            http.DefaultRequestHeaders.UserAgent.TryParseAdd(hvvOptions.UserAgent);
        }
    });
    builder.Services.AddHostedService<HvvRefreshService>();

    // Strava / Lauf-Heatmap (Phase 7)
    var stravaOptions = builder.Configuration
        .GetSection(StravaOptions.SectionName)
        .Get<StravaOptions>() ?? new StravaOptions();

    builder.Services.Configure<StravaOptions>(
        builder.Configuration.GetSection(StravaOptions.SectionName));
    builder.Services.AddScoped<IRunRepository, RunRepository>();
    builder.Services.AddScoped<IStravaTokenStore, StravaTokenStore>();
    builder.Services.AddScoped<ISyncStateStore, SyncStateStore>();
    builder.Services.AddHttpClient<StravaTokenService>(http =>
    {
        http.BaseAddress = new Uri(stravaOptions.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(20);
    });
    builder.Services.AddScoped<IStravaAccessTokenProvider>(
        sp => sp.GetRequiredService<StravaTokenService>());
    builder.Services.AddHttpClient<IStravaActivityProvider, StravaClient>(http =>
    {
        http.BaseAddress = new Uri(stravaOptions.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(20);
    });
    builder.Services.AddHostedService<StravaSyncService>();

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

    // Strava-OAuth (Phase 7): Login-Redirect und Callback zum Token-Tausch.
    // Der state-Parameter (zufällig, in einem kurzlebigen Cookie gespiegelt) schützt
    // den Callback gegen CSRF/Token-Injection.
    const string stravaStateCookie = "strava_oauth_state";

    app.MapGet("/strava/connect", (HttpContext httpContext, StravaTokenService tokenService) =>
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        httpContext.Response.Cookies.Append(stravaStateCookie, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
            IsEssential = true
        });
        return Results.Redirect(tokenService.BuildAuthorizeUrl(state).ToString());
    });

    app.MapGet("/strava/callback", async (
        HttpContext httpContext, StravaTokenService tokenService, CancellationToken ct) =>
    {
        var code = httpContext.Request.Query["code"].ToString();
        var error = httpContext.Request.Query["error"].ToString();
        var state = httpContext.Request.Query["state"].ToString();
        var expectedState = httpContext.Request.Cookies[stravaStateCookie];
        httpContext.Response.Cookies.Delete(stravaStateCookie);

        var stateValid = !string.IsNullOrEmpty(expectedState)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(expectedState));

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || !stateValid)
        {
            return Results.Redirect("/heatmap");
        }

        await tokenService.ExchangeCodeAsync(code, ct);
        return Results.Redirect("/heatmap");
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
