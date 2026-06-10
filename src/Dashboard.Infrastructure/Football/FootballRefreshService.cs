using Dashboard.Domain.Football;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Football;

/// <summary>
/// Pollt die Fußball-API im konfigurierten Intervall und legt das Ergebnis in <see cref="FootballState"/> ab.
/// Bei Fehlern bleiben die letzten Daten erhalten (Graceful Degradation); ohne API-Key oder ohne
/// konfigurierte Vereine beendet sich der Dienst geordnet.
/// </summary>
public sealed class FootballRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FootballState _state;
    private readonly FootballOptions _options;
    private readonly ILogger<FootballRefreshService> _logger;

    public FootballRefreshService(
        IServiceScopeFactory scopeFactory,
        FootballState state,
        IOptions<FootballOptions> options,
        ILogger<FootballRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _state = state;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || _options.Teams.Count == 0)
        {
            _logger.LogWarning(
                "Fußball: Kein API-Key oder keine Vereine konfiguriert – die Fußball-Kachel bleibt im " +
                "\"nicht verfügbar\"-Zustand. Key via User-Secrets setzen (Football:ApiKey).");
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
            var provider = scope.ServiceProvider.GetRequiredService<IFootballProvider>();

            var snapshot = await provider.GetFootballAsync(ct);
            _state.Update(snapshot);
            _logger.LogInformation("Fußball: Snapshot aktualisiert ({Teams} Vereine).", snapshot.Teams.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fußball: Aktualisierung fehlgeschlagen – letzte Daten bleiben erhalten.");
            _state.MarkStale();
        }
    }
}
