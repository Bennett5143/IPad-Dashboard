namespace Dashboard.Domain.Whoop;

/// <summary>
/// Persistenz der WHOOP-Workouts (FA-9.12), fortlaufend vom Hintergrund-Sync befüllt.
/// Quelle für Querschnitts-Auswertungen wie die Tageszeit-Effektivität (FA-10.01),
/// ohne je Auswertung die WHOOP-API abfragen zu müssen.
/// </summary>
public interface IWhoopWorkoutStore
{
    /// <summary>
    /// Legt Workouts an bzw. aktualisiert sie vollständig anhand der WHOOP-UUID
    /// (Scores werden nachträglich finalisiert).
    /// </summary>
    Task UpsertAsync(IReadOnlyList<WhoopWorkout> workouts, CancellationToken ct = default);

    /// <summary>Workouts mit Start im Bereich, aufsteigend nach Startzeit.</summary>
    Task<IReadOnlyList<WhoopWorkout>> GetRangeAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct = default);

    /// <summary>Früheste gespeicherte Startzeit; <c>null</c>, solange der Store leer ist.</summary>
    Task<DateTimeOffset?> GetOldestStartUtcAsync(CancellationToken ct = default);
}
