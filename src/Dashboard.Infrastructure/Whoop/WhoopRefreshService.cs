using Dashboard.Domain.Time;
using Dashboard.Domain.Whoop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Whoop;

/// <summary>
/// Pollt die WHOOP-API im konfigurierten Intervall und legt das Ergebnis in <see cref="WhoopState"/> ab.
/// Persistiert zusätzlich die Tageswerte (FA-9.10): je Zyklus das jüngste Fenster plus höchstens
/// ein historisches Backfill-Fenster. Bei Fehlern bleiben die letzten Daten erhalten (Graceful
/// Degradation); ohne ClientId/Secret beendet sich der Dienst geordnet; solange nicht verbunden
/// (keine Tokens) wird still übersprungen.
/// </summary>
public sealed class WhoopRefreshService : BackgroundService
{
    private const int RecentWindowDays = 30;
    private const int BackfillWindowDays = 90;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WhoopState _state;
    private readonly WhoopOptions _options;
    private readonly ILogger<WhoopRefreshService> _logger;

    // Backfill-Fortschritt bewusst nur in-memory: Nach einem Neustart beginnt er wieder beim
    // ältesten gespeicherten Stand — der Upsert ist idempotent, es kostet höchstens erneute Abrufe.
    private DateTimeOffset? _backfillCursor;
    private bool _backfillExhausted;
    private DateTimeOffset? _workoutBackfillCursor;
    private bool _workoutBackfillExhausted;

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

            // Tageswerte persistieren (FA-9.10) – ebenfalls vom Snapshot entkoppelt.
            try
            {
                await PersistHistoryAsync(scope.ServiceProvider, provider, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "WHOOP: Persistieren der Tageswerte fehlgeschlagen.");
            }

            // Workouts persistieren (FA-9.12) – Grundlage der Tageszeit-Auswertungen.
            try
            {
                await PersistWorkoutsAsync(scope.ServiceProvider, provider, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "WHOOP: Persistieren der Workouts fehlgeschlagen.");
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

    /// <summary>
    /// Hält die Tabelle der Tageswerte aktuell: das jüngste Fenster deckt nachträgliches
    /// Re-Scoring ab; danach höchstens ein Backfill-Fenster rückwärts pro Zyklus, bis die
    /// konfigurierte Tiefe erreicht ist. Leere Fenster (Band nicht getragen) rücken den
    /// Cursor einfach weiter.
    /// </summary>
    private async Task PersistHistoryAsync(
        IServiceProvider services, IWhoopProvider provider, CancellationToken ct)
    {
        var metricStore = services.GetRequiredService<IWhoopMetricStore>();
        var now = services.GetRequiredService<IClock>().UtcNow;

        var recent = await provider.GetHistoryAsync(now.AddDays(-RecentWindowDays), now, ct);
        await metricStore.UpsertAsync(recent, ct);

        if (_backfillExhausted)
        {
            return;
        }

        if (_backfillCursor is null)
        {
            var oldest = await metricStore.GetOldestDateAsync(ct);
            if (oldest is null)
            {
                return; // Store noch leer (keine Daten im jüngsten Fenster) – später erneut versuchen
            }
            _backfillCursor = WhoopBackfillPlanner.StartOfBerlinDay(oldest.Value);
        }

        var window = WhoopBackfillPlanner.NextWindow(
            now, _backfillCursor.Value, _options.BackfillDays, BackfillWindowDays);
        if (window is null)
        {
            _backfillExhausted = true;
            _logger.LogInformation(
                "WHOOP: Historien-Backfill abgeschlossen (Tiefe {Days} Tage erreicht).",
                _options.BackfillDays);
            return;
        }

        var older = await provider.GetHistoryAsync(window.FromUtc, window.ToUtc, ct);
        await metricStore.UpsertAsync(older, ct);
        _backfillCursor = window.FromUtc;
        _logger.LogInformation(
            "WHOOP: Historien-Backfill – {Count} Tag(e) im Fenster ab {From:yyyy-MM-dd} übernommen.",
            older.Count, window.FromUtc);
    }

    /// <summary>
    /// Hält die Workout-Tabelle aktuell — gleiches Schema wie <see cref="PersistHistoryAsync"/>:
    /// jüngstes Fenster (Scores werden nachträglich finalisiert) plus höchstens ein
    /// Backfill-Fenster rückwärts pro Zyklus.
    /// </summary>
    private async Task PersistWorkoutsAsync(
        IServiceProvider services, IWhoopProvider provider, CancellationToken ct)
    {
        var workoutStore = services.GetRequiredService<IWhoopWorkoutStore>();
        var now = services.GetRequiredService<IClock>().UtcNow;

        var recent = await provider.GetWorkoutsAsync(now.AddDays(-RecentWindowDays), now, ct);
        await workoutStore.UpsertAsync(recent, ct);

        if (_workoutBackfillExhausted)
        {
            return;
        }

        if (_workoutBackfillCursor is null)
        {
            var oldest = await workoutStore.GetOldestStartUtcAsync(ct);
            if (oldest is null)
            {
                return; // Store noch leer (keine Workouts im jüngsten Fenster) – später erneut versuchen
            }
            _workoutBackfillCursor = oldest.Value;
        }

        var window = WhoopBackfillPlanner.NextWindow(
            now, _workoutBackfillCursor.Value, _options.BackfillDays, BackfillWindowDays);
        if (window is null)
        {
            _workoutBackfillExhausted = true;
            _logger.LogInformation(
                "WHOOP: Workout-Backfill abgeschlossen (Tiefe {Days} Tage erreicht).",
                _options.BackfillDays);
            return;
        }

        var older = await provider.GetWorkoutsAsync(window.FromUtc, window.ToUtc, ct);
        await workoutStore.UpsertAsync(older, ct);
        _workoutBackfillCursor = window.FromUtc;
        _logger.LogInformation(
            "WHOOP: Workout-Backfill – {Count} Workout(s) im Fenster ab {From:yyyy-MM-dd} übernommen.",
            older.Count, window.FromUtc);
    }
}
