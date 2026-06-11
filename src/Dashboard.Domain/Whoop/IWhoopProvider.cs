namespace Dashboard.Domain.Whoop;

/// <summary>Holt den aktuellen WHOOP-Tagesstatus (Recovery, Schlaf, Tages-Strain) sowie Workouts.</summary>
public interface IWhoopProvider
{
    Task<WhoopSnapshot> GetWhoopAsync(CancellationToken ct = default);

    /// <summary>Workouts im Zeitfenster [<paramref name="fromUtc"/>, <paramref name="toUtc"/>].</summary>
    Task<IReadOnlyList<WhoopWorkout>> GetWorkoutsAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct = default);
}
