using System.Security.Cryptography;
using System.Text;

using Dashboard.Infrastructure;
using Dashboard.Infrastructure.Crests;
using Dashboard.Infrastructure.Crypto;
using Dashboard.Infrastructure.Football;
using Dashboard.Infrastructure.Habits;
using Dashboard.Infrastructure.Hvv;
using Dashboard.Infrastructure.Quotes;
using Dashboard.Infrastructure.Seeding;
using Dashboard.Infrastructure.Status;
using Dashboard.Infrastructure.Strava;
using Dashboard.Infrastructure.Tiles;
using Dashboard.Infrastructure.Time;
using Dashboard.Infrastructure.Weather;
using Dashboard.Infrastructure.Whoop;
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

    // Lokale, nicht versionierte Overrides (z. B. private HVV-Haltestellen / Standort).
    // Umgebungsunabhängig (lädt auch in Production/auf dem Pi), daher hier explizit registriert.
    // Die Datei ist gitignored; als Vorlage dient appsettings.Local.json.example.
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

    // Ringpuffer der jüngsten Warnungen/Fehler für die Status-Seite (FA-11.03).
    var recentLogBuffer = new RecentLogBuffer();
    builder.Services.AddSingleton(recentLogBuffer);
    builder.Services.AddSingleton<IRecentLogProvider>(recentLogBuffer);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Sink(new RingBufferLogSink(recentLogBuffer)));

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
    // Fabrizio-Alert-Seam: bis 15.8 (echter Social-Feed) ein Null-Object → die Summary-Kachel
    // hält den Badge-Slot bereit, ohne dass etwas erscheint.
    builder.Services.AddSingleton<IFabrizioAlertSource, NullFabrizioAlertSource>();
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

    // Crypto (Phase 15.4): Markt-Watchlist (CoinGecko) + Marktstimmung (alternative.me).
    // Beide Quellen frei & schlüssellos; das offline-iPad bekommt alles serverseitig gepusht.
    var cryptoOptions = builder.Configuration
        .GetSection(CryptoOptions.SectionName)
        .Get<CryptoOptions>() ?? new CryptoOptions();

    builder.Services.Configure<CryptoOptions>(
        builder.Configuration.GetSection(CryptoOptions.SectionName));
    builder.Services.AddSingleton<CryptoState>();
    builder.Services.AddHttpClient<ICryptoMarketProvider, CoinGeckoClient>(http =>
    {
        http.BaseAddress = new Uri(cryptoOptions.MarketBaseUrl);
        http.Timeout = TimeSpan.FromSeconds(10);
    });
    builder.Services.AddHttpClient<IMarketSentimentProvider, FearGreedClient>(http =>
    {
        http.BaseAddress = new Uri(cryptoOptions.SentimentBaseUrl);
        http.Timeout = TimeSpan.FromSeconds(10);
    });
    builder.Services.AddHostedService<CryptoRefreshService>();

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
    builder.Services.AddScoped<IRouteClusterStore, RouteClusterStore>();
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

    // WHOOP / Recovery-Readiness (Phase 8)
    var whoopOptions = builder.Configuration
        .GetSection(WhoopOptions.SectionName)
        .Get<WhoopOptions>() ?? new WhoopOptions();

    builder.Services.Configure<WhoopOptions>(
        builder.Configuration.GetSection(WhoopOptions.SectionName));
    builder.Services.AddSingleton<WhoopState>();
    builder.Services.AddScoped<IWhoopTokenStore, WhoopTokenStore>();
    builder.Services.AddHttpClient<WhoopTokenService>(http =>
    {
        http.BaseAddress = new Uri(whoopOptions.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(20);
    });
    builder.Services.AddScoped<IWhoopAccessTokenProvider>(
        sp => sp.GetRequiredService<WhoopTokenService>());
    builder.Services.AddHttpClient<IWhoopProvider, WhoopClient>(http =>
    {
        http.BaseAddress = new Uri(whoopOptions.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(20);
    });
    builder.Services.AddScoped<IWhoopProcessedWorkoutStore, WhoopProcessedWorkoutStore>();
    builder.Services.AddScoped<IWhoopMetricStore, WhoopMetricStore>();
    builder.Services.AddScoped<IWhoopWorkoutStore, WhoopWorkoutStore>();
    builder.Services.AddScoped<WhoopHabitSync>();
    builder.Services.AddHostedService<WhoopRefreshService>();

    // Habits
    builder.Services.AddScoped<IHabitEntryRepository, HabitEntryRepository>();
    builder.Services.AddScoped<HabitTrackingService>();

    // Status (Phase 13.1): dieselben State-Singletons zusätzlich als Status-Quellen
    // registrieren; Host-Metriken nur unter Linux (Raspberry Pi), sonst bewusst leer.
    builder.Services.AddSingleton<ISliceStatusSource>(sp => sp.GetRequiredService<WeatherState>());
    builder.Services.AddSingleton<ISliceStatusSource>(sp => sp.GetRequiredService<FootballState>());
    builder.Services.AddSingleton<ISliceStatusSource>(sp => sp.GetRequiredService<CryptoState>());
    builder.Services.AddSingleton<ISliceStatusSource>(sp => sp.GetRequiredService<HvvState>());
    builder.Services.AddSingleton<ISliceStatusSource>(sp => sp.GetRequiredService<WhoopState>());
    builder.Services.AddSingleton<ISystemMetricsProvider>(OperatingSystem.IsLinux()
        ? new LinuxSystemMetricsProvider()
        : new NullSystemMetricsProvider());

    // Karten-Kachel-Proxy: Das bewusst offline gehaltene Kiosk-iPad holt Kacheln nur vom
    // LAN-Server, der sie online nachlädt und auf Platte cached (Sektion „Tiles").
    var tileOptions = builder.Configuration
        .GetSection(TileOptions.SectionName)
        .Get<TileOptions>() ?? new TileOptions();

    builder.Services.Configure<TileOptions>(builder.Configuration.GetSection(TileOptions.SectionName));
    builder.Services.Configure<MapOptions>(builder.Configuration.GetSection(MapOptions.SectionName));
    builder.Services.AddHttpClient<TileProvider>(http =>
    {
        http.Timeout = TimeSpan.FromSeconds(15);
        http.DefaultRequestHeaders.UserAgent.TryParseAdd(tileOptions.UserAgent);
    });

    // Wappen-/Flaggen-Proxy: gleicher Offline-Gedanke wie beim Kachel-Proxy. Vereinswappen und
    // Nationalflaggen (football-data.org) werden server-seitig geholt/gecacht und über /crests
    // ausgeliefert, damit das iPad kein Bild direkt aus dem Internet laden muss (Sektion „Crests").
    var crestOptions = builder.Configuration
        .GetSection(CrestOptions.SectionName)
        .Get<CrestOptions>() ?? new CrestOptions();

    builder.Services.Configure<CrestOptions>(builder.Configuration.GetSection(CrestOptions.SectionName));
    builder.Services.AddHttpClient<CrestProvider>(http =>
    {
        http.Timeout = TimeSpan.FromSeconds(15);
        http.DefaultRequestHeaders.UserAgent.TryParseAdd(crestOptions.UserAgent);
    });

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

    // WHOOP-OAuth (Phase 8): gleiches state-Cookie-/CSRF-Muster wie bei Strava.
    const string whoopStateCookie = "whoop_oauth_state";

    app.MapGet("/whoop/connect", (HttpContext httpContext, WhoopTokenService tokenService) =>
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        httpContext.Response.Cookies.Append(whoopStateCookie, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
            IsEssential = true
        });
        return Results.Redirect(tokenService.BuildAuthorizeUrl(state).ToString());
    });

    app.MapGet("/whoop/callback", async (
        HttpContext httpContext,
        WhoopTokenService tokenService,
        IWhoopProvider whoopProvider,
        WhoopState whoopState,
        ILoggerFactory loggerFactory,
        CancellationToken ct) =>
    {
        var code = httpContext.Request.Query["code"].ToString();
        var error = httpContext.Request.Query["error"].ToString();
        var state = httpContext.Request.Query["state"].ToString();
        var expectedState = httpContext.Request.Cookies[whoopStateCookie];
        httpContext.Response.Cookies.Delete(whoopStateCookie);

        var stateValid = !string.IsNullOrEmpty(expectedState)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(expectedState));

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || !stateValid)
        {
            return Results.Redirect("/");
        }

        await tokenService.ExchangeCodeAsync(code, ct);

        // Direkt nach dem Verbinden einen ersten Snapshot holen, damit die Kachel sofort
        // befüllt ist – statt bis zum nächsten Hintergrund-Poll (alle 30 min) zu warten.
        try
        {
            whoopState.Update(await whoopProvider.GetWhoopAsync(ct));
        }
        catch (Exception ex)
        {
            loggerFactory.CreateLogger("Whoop").LogWarning(
                ex, "WHOOP: Erst-Abruf nach Verbindung fehlgeschlagen – der Hintergrund-Sync holt es nach.");
        }

        return Results.Redirect("/");
    });

    // Karten-Kachel-Proxy-Endpoint: liefert Kacheln aus dem lokalen Cache bzw. lädt sie online
    // nach. Slippy-Map-Schema {z}/{x}/{y}.png; Leaflet zeigt eine fehlende Kachel einfach nicht.
    app.MapGet("/tiles/{z:int}/{x:int}/{y:int}.png", async (
        int z, int x, int y, TileProvider tiles, HttpContext context, CancellationToken ct) =>
    {
        var bytes = await tiles.GetTileAsync(z, x, y, ct);
        if (bytes is null)
        {
            // Fehlversuch NICHT cachen → der nächste Map-Load fragt die Kachel erneut an.
            context.Response.Headers.CacheControl = "no-store";
            return Results.NotFound();
        }

        context.Response.Headers.CacheControl = "public, max-age=2592000"; // 30 Tage im Client-Cache
        return Results.File(bytes, "image/png");
    });

    // Wappen-/Flaggen-Proxy-Endpoint: nimmt die Upstream-URL als Query-Parameter (?u=…), prüft sie
    // gegen die Host-Allowlist und liefert das gecachte Bild. So bleibt das iPad offline.
    app.MapGet("/crests", async (
        string u, CrestProvider crests, HttpContext context, CancellationToken ct) =>
    {
        var image = await crests.GetCrestAsync(u, ct);
        if (image is null)
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.NotFound();
        }

        context.Response.Headers.CacheControl = "public, max-age=2592000"; // 30 Tage im Client-Cache
        return Results.File(image.Bytes, image.ContentType);
    });

    // Cache vorwärmen: lädt ein Gebiet (Bounding-Box × Zoomstufen) einmalig gedrosselt in den
    // Cache, damit die Karte danach auch offline lückenlos und sofort ist. Läuft im Hintergrund;
    // Fortschritt im Server-Log. Mengen-Schranke gegen versehentlich riesige Anfragen.
    app.MapGet("/tiles/warm", (
        double minLat, double minLng, double maxLat, double maxLng, int minZoom, int maxZoom,
        IServiceScopeFactory scopeFactory, ILoggerFactory loggerFactory) =>
    {
        if (minZoom < 0 || maxZoom > 19 || minZoom > maxZoom || minLat >= maxLat || minLng >= maxLng)
        {
            return Results.BadRequest("Ungültige Parameter (Zoom 0–19, minZoom ≤ maxZoom, min < max).");
        }

        var count = TileProvider.CountTiles(minLat, minLng, maxLat, maxLng, minZoom, maxZoom);
        if (count > 200_000)
        {
            return Results.BadRequest($"Zu viele Kacheln ({count:N0}). Kleinere Fläche oder weniger Zoom wählen.");
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<TileProvider>();
                await provider.WarmAsync(minLat, minLng, maxLat, maxLng, minZoom, maxZoom, CancellationToken.None);
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger("TileWarm").LogWarning(ex, "Tile-Warmup abgebrochen.");
            }
        });

        return Results.Ok($"Warmup gestartet: ~{count:N0} Kacheln. Fortschritt im Server-Log.");
    });

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
        // HTTPS nur außerhalb von Development erzwingen. In Development lauscht die App
        // zusätzlich per HTTP auf allen Interfaces (LAN-Kiosk vom iPad); ein Redirect würde
        // dort auf das nur für localhost gültige Dev-Zertifikat verweisen.
        app.UseHttpsRedirection();
    }
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

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
