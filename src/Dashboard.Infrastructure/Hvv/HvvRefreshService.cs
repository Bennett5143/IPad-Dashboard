using Dashboard.Domain.Hvv;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Hvv;

/// <summary>
/// Pollt den HVV-Endpoint konservativ (min. 60 s, FA-6.04) und legt die Abfahrten in
/// <see cref="HvvState"/> ab. Bei Fehlern bleiben die letzten Daten erhalten (Graceful Degradation);
/// ohne konfigurierte Haltestellen beendet sich der Dienst geordnet.
/// </summary>
public sealed class HvvRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HvvState _state;
    private readonly HvvOptions _options;
    private readonly ILogger<HvvRefreshService> _logger;

    public HvvRefreshService(
        IServiceScopeFactory scopeFactory,
        HvvState state,
        IOptions<HvvOptions> options,
        ILogger<HvvRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _state = state;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Stations.Count == 0)
        {
            _logger.LogWarning(
                "HVV: Keine Haltestellen konfiguriert – der Abfahrtsmonitor bleibt im " +
                "\"nicht verfügbar\"-Zustand. Haltestellen in appsettings.json (Sektion Hvv) ergänzen.");
            return;
        }

        // FA-6.04: niemals häufiger als einmal pro Minute pro Haltestelle pollen.
        var interval = TimeSpan.FromSeconds(Math.Max(60, _options.PollIntervalSeconds));
        using var timer = new PeriodicTimer(interval);
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
            var provider = scope.ServiceProvider.GetRequiredService<IHvvProvider>();

            var snapshot = await provider.GetDeparturesAsync(ct);
            _state.Update(snapshot);
            _logger.LogInformation("HVV: Abfahrten aktualisiert ({Stations} Haltestellen).", snapshot.Stations.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HVV: Aktualisierung fehlgeschlagen – letzte Daten bleiben erhalten.");
            _state.MarkStale();
        }
    }
}
