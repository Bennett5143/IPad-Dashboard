namespace Dashboard.Domain.Running;

/// <summary>
/// Holt Lauf-Activities von Strava (bereits gefiltert auf Run/TrailRun, Polyline dekodiert).
/// <paramref name="afterUtc"/> begrenzt auf Läufe nach diesem Zeitpunkt (inkrementeller Sync, FA-8.07).
/// </summary>
public interface IStravaActivityProvider
{
    Task<IReadOnlyList<Run>> GetActivitiesAsync(DateTimeOffset? afterUtc, CancellationToken ct = default);
}
