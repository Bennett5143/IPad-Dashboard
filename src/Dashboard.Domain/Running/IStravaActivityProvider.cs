namespace Dashboard.Domain.Running;

/// <summary>
/// Holt Lauf-Activities von Strava (bereits gefiltert auf Run/TrailRun, Polyline dekodiert).
/// Mit <c>afterUtc</c> nur Läufe nach diesem Zeitpunkt (inkrementeller Sync, FA-8.07).
/// </summary>
public interface IStravaActivityProvider
{
    /// <param name="afterUtc">Nur Läufe nach diesem Zeitpunkt; <c>null</c> lädt alle (Erst-Sync).</param>
    /// <param name="ct">Abbruch-Token.</param>
    Task<IReadOnlyList<Run>> GetActivitiesAsync(DateTimeOffset? afterUtc, CancellationToken ct = default);
}
