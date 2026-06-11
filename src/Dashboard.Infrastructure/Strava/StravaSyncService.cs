using Dashboard.Domain.Running;
using Dashboard.Domain.Time;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Strava;

/// <summary>
/// Synchronisiert Strava-Läufe periodisch in die lokale DB. Inkrementell ab dem jüngsten
/// gespeicherten Lauf (FA-8.07); Erst-Sync ohne <c>after</c> lädt die Historie. Fehler werden im
/// Sync-Zustand vermerkt (FA-8.09); ohne Verbindung passiert nichts.
/// </summary>
public sealed class StravaSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly StravaOptions _options;
    private readonly ILogger<StravaSyncService> _logger;

    public StravaSyncService(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        IOptions<StravaOptions> options,
        ILogger<StravaSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.SyncInterval);
        try
        {
            do
            {
                await SyncOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Regulärer Shutdown.
        }
    }

    private async Task SyncOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IStravaTokenStore>();

        if (!await tokens.HasTokensAsync(ct))
        {
            _logger.LogDebug("Strava: Nicht verbunden – Sync übersprungen.");
            return;
        }

        var provider = scope.ServiceProvider.GetRequiredService<IStravaActivityProvider>();
        var repository = scope.ServiceProvider.GetRequiredService<IRunRepository>();
        var syncState = scope.ServiceProvider.GetRequiredService<ISyncStateStore>();

        try
        {
            var after = await repository.GetLatestRunStartAsync(ct);
            var runs = await provider.GetActivitiesAsync(after, ct);
            await repository.UpsertAsync(runs, ct);
            await syncState.RecordSuccessAsync(_clock.UtcNow, ct);
            _logger.LogInformation("Strava: {Count} Läufe synchronisiert (ab {After:o}).", runs.Count, after);

            await BackfillStreamsAsync(provider, repository, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strava: Sync fehlgeschlagen – letzter Stand bleibt erhalten.");
            await syncState.RecordFailureAsync(ex.Message, _clock.UtcNow, ct);
        }
    }

    // Pro-Punkt-Streams (Höhe/HF/Zeit) für die erweiterten Heatmap-Ebenen nachladen.
    // Bewusst gedrosselt (ein API-Call je Lauf), pausiert bei Rate-Limit und füllt über
    // mehrere Sync-Zyklen nach. Darf den eigentlichen Lauf-Sync nicht gefährden.
    private const int MaxStreamsPerCycle = 20;

    private async Task BackfillStreamsAsync(
        IStravaActivityProvider provider, IRunRepository repository, CancellationToken ct)
    {
        var ids = await repository.GetIdsMissingStreamsAsync(MaxStreamsPerCycle, ct);
        if (ids.Count == 0)
        {
            return;
        }

        var done = 0;
        foreach (var id in ids)
        {
            try
            {
                var streams = await provider.GetStreamsAsync(id, ct);
                await repository.SaveStreamsAsync(id, streams, ct);
                done++;
            }
            catch (StravaRateLimitException ex)
            {
                _logger.LogInformation(
                    "Strava: Stream-Backfill pausiert ({Reason}) – {Done}/{Total} in diesem Zyklus.",
                    ex.Message, done, ids.Count);
                break;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Strava: Streams für Lauf {Id} übersprungen.", id);
            }
        }

        if (done > 0)
        {
            _logger.LogInformation("Strava: Streams für {Done} Läufe nachgeladen.", done);
        }
    }
}
