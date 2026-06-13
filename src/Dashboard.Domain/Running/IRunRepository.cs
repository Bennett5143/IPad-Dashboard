namespace Dashboard.Domain.Running;

/// <summary>Persistenz der synchronisierten Läufe in der lokalen DB (PostGIS in der Infrastruktur).</summary>
public interface IRunRepository
{
    /// <summary>Fügt neue Läufe ein bzw. aktualisiert bestehende (per Strava-Id).</summary>
    Task UpsertAsync(IReadOnlyList<Run> runs, CancellationToken ct = default);

    /// <summary>Läufe ab <paramref name="sinceUtc"/> (oder alle), neueste zuerst.</summary>
    Task<IReadOnlyList<Run>> GetRunsAsync(DateTimeOffset? sinceUtc, CancellationToken ct = default);

    /// <summary>
    /// Wie <see cref="GetRunsAsync"/>, aber ohne Track/Streams (leerer Track) — für
    /// Auswertungen und Listen, die nur Metriken brauchen; lädt keine PostGIS-Geometrie.
    /// </summary>
    Task<IReadOnlyList<Run>> GetRunSummariesAsync(DateTimeOffset? sinceUtc, CancellationToken ct = default);

    /// <summary>Ein einzelner Lauf (mit Track/Streams, sofern vorhanden); <c>null</c>, wenn unbekannt.</summary>
    Task<Run?> GetRunAsync(long id, CancellationToken ct = default);

    /// <summary>Startzeit des jüngsten gespeicherten Laufs – Basis für den inkrementellen Sync.</summary>
    Task<DateTimeOffset?> GetLatestRunStartAsync(CancellationToken ct = default);

    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>Ids gespeicherter Läufe, deren Streams noch nicht abgerufen wurden (neueste zuerst).</summary>
    Task<IReadOnlyList<long>> GetIdsMissingStreamsAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// Speichert die Streams eines Laufs und markiert ihn als abgerufen (auch bei <c>null</c>, damit
    /// Läufe ohne Stream-Daten nicht endlos erneut versucht werden). Ersetzt die Route durch die
    /// volle Auflösung aus dem latlng-Stream.
    /// </summary>
    Task SaveStreamsAsync(long runId, StravaStreams? streams, CancellationToken ct = default);
}
