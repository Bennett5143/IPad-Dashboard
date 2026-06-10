namespace Dashboard.Domain.Running;

/// <summary>Persistenz der synchronisierten Läufe in der lokalen DB (PostGIS in der Infrastruktur).</summary>
public interface IRunRepository
{
    /// <summary>Fügt neue Läufe ein bzw. aktualisiert bestehende (per Strava-Id).</summary>
    Task UpsertAsync(IReadOnlyList<Run> runs, CancellationToken ct = default);

    /// <summary>Läufe ab <paramref name="sinceUtc"/> (oder alle), neueste zuerst.</summary>
    Task<IReadOnlyList<Run>> GetRunsAsync(DateTimeOffset? sinceUtc, CancellationToken ct = default);

    /// <summary>Startzeit des jüngsten gespeicherten Laufs – Basis für den inkrementellen Sync.</summary>
    Task<DateTimeOffset?> GetLatestRunStartAsync(CancellationToken ct = default);

    Task<int> CountAsync(CancellationToken ct = default);
}
