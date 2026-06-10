using Dashboard.Domain.Weather;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Weather;

/// <summary>
/// Pollt die Wetter-API in festem Intervall und legt das Ergebnis in <see cref="WeatherState"/> ab.
/// Bei Fehlern bleiben die letzten Daten erhalten (Graceful Degradation); ohne konfigurierten
/// API-Key beendet sich der Dienst geordnet, ohne den Start der App zu behindern.
/// </summary>
public sealed class WeatherRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WeatherState _state;
    private readonly WeatherOptions _options;
    private readonly ILogger<WeatherRefreshService> _logger;

    public WeatherRefreshService(
        IServiceScopeFactory scopeFactory,
        WeatherState state,
        IOptions<WeatherOptions> options,
        ILogger<WeatherRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _state = state;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning(
                "Wetter: Kein API-Key konfiguriert – die Wetter-Kacheln bleiben im " +
                "\"nicht verfügbar\"-Zustand. Key via User-Secrets/Umgebungsvariable setzen (Weather:ApiKey).");
            return;
        }

        using var timer = new PeriodicTimer(_options.RefreshInterval);
        try
        {
            do
            {
                await RefreshAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Regulärer Shutdown.
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var provider = scope.ServiceProvider.GetRequiredService<IWeatherProvider>();

            var snapshot = await provider.GetWeatherAsync(ct);
            _state.Update(snapshot);
            _logger.LogInformation("Wetter: Snapshot aktualisiert.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wetter: Aktualisierung fehlgeschlagen – letzte Daten bleiben erhalten.");
            _state.MarkStale();
        }
    }
}
