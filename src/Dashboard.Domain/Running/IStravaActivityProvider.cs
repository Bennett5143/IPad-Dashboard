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

    /// <summary>
    /// Pro-Punkt-Streams (latlng/Zeit/Höhe/HF) einer Aktivität; <c>null</c>, wenn keine nutzbaren
    /// Daten (z. B. Indoor-Lauf ohne GPS). Wirft bei Rate-Limit, damit der Backfill pausieren kann.
    /// </summary>
    Task<StravaStreams?> GetStreamsAsync(long activityId, CancellationToken ct = default);
}
