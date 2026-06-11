using Dashboard.Domain.Whoop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Whoop;

/// <summary>
/// Pollt die WHOOP-API im konfigurierten Intervall und legt das Ergebnis in <see cref="WhoopState"/> ab.
/// Bei Fehlern bleiben die letzten Daten erhalten (Graceful Degradation); ohne ClientId/Secret beendet
/// sich der Dienst geordnet; solange nicht verbunden (keine Tokens) wird still übersprungen.
/// </summary>
public sealed class WhoopRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WhoopState _state;
    private readonly WhoopOptions _options;
    private readonly ILogger<WhoopRefreshService> _logger;

    public WhoopRefreshService(
        IServiceScopeFactory scopeFactory,
        WhoopState state,
        IOptions<WhoopOptions> options,
        ILogger<WhoopRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _state = state;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            _logger.LogWarning(
                "WHOOP: Keine ClientId/ClientSecret konfiguriert – die WHOOP-Kachel bleibt leer. " +
                "Secrets via User-Secrets setzen (Whoop:ClientId / Whoop:ClientSecret).");
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

            var store = scope.ServiceProvider.GetRequiredService<IWhoopTokenStore>();
            if (!await store.HasTokensAsync(ct))
            {
                _logger.LogDebug("WHOOP: nicht verbunden – Aktualisierung übersprungen (verbinden über /whoop/connect).");
                return;
            }

            var provider = scope.ServiceProvider.GetRequiredService<IWhoopProvider>();
            var snapshot = await provider.GetWhoopAsync(ct);
            _state.Update(snapshot);
            _logger.LogInformation(
                "WHOOP: Snapshot aktualisiert (Recovery {Recovery}%).", snapshot.Recovery?.ScorePercent);

            // Heutige Workouts in Habits übernehmen – darf den Recovery-Snapshot nicht gefährden.
            try
            {
                var applied = await scope.ServiceProvider
                    .GetRequiredService<WhoopHabitSync>().ApplyTodayAsync(ct);
                if (applied > 0)
                {
                    _logger.LogInformation("WHOOP: {Count} Habit(s) aus heutigen Workouts übernommen.", applied);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "WHOOP: Habit-Übernahme aus Workouts fehlgeschlagen.");
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WHOOP: Aktualisierung fehlgeschlagen – letzte Daten bleiben erhalten.");
            _state.MarkStale();
        }
    }
}
