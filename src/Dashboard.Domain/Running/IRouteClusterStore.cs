namespace Dashboard.Domain.Running;

/// <summary>Anzeige-Aggregat einer „Standard-Runde" (FA-8.17).</summary>
public sealed record RouteClusterSummary(
    int Id,
    string Name,
    int MemberCount,
    double AverageDistanceKm,
    double? AveragePaceMinPerKm,
    TimeSpan? BestTime);

/// <summary>Zuordnung eines Laufs zu seiner Runde (für das Detail-Badge).</summary>
public sealed record RouteClusterInfo(int Id, string Name);

/// <summary>
/// Persistenz der Routen-Cluster (FA-8.17). Repräsentanten + Zuordnung werden vom
/// Hintergrund-Sync gepflegt; die Lese-Methoden speisen die UI.
/// </summary>
public interface IRouteClusterStore
{
    /// <summary>Repräsentanten aller Cluster (Erst-Lauf je Cluster) inkl. Track für den Vergleich.</summary>
    Task<IReadOnlyList<RouteClusterRepresentative>> GetRepresentativesAsync(CancellationToken ct = default);

    /// <summary>Ids noch nicht zugeordneter Läufe mit Track, älteste zuerst (deterministische Reihenfolge).</summary>
    Task<IReadOnlyList<long>> GetUnmatchedRunIdsAsync(int limit, CancellationToken ct = default);

    /// <summary>Legt einen neuen Cluster mit diesem Lauf als Repräsentant an und gibt die Id zurück.</summary>
    Task<int> CreateClusterAsync(
        long representativeRunId, double distanceMeters, DateTimeOffset whenUtc, CancellationToken ct = default);

    /// <summary>Ordnet einen Lauf einem bestehenden Cluster zu (erhöht die Mitgliederzahl).</summary>
    Task AssignAsync(long runId, int clusterId, DateTimeOffset whenUtc, CancellationToken ct = default);

    /// <summary>Markiert einen Lauf ohne nutzbaren Track als bearbeitet (kein erneuter Versuch).</summary>
    Task MarkUnclusterableAsync(long runId, DateTimeOffset whenUtc, CancellationToken ct = default);

    /// <summary>Runden-Übersicht (Anzahl, Ø-Distanz/-Pace, Bestzeit), absteigend nach Mitgliederzahl.</summary>
    Task<IReadOnlyList<RouteClusterSummary>> GetSummariesAsync(CancellationToken ct = default);

    /// <summary>Runde eines Laufs; <c>null</c>, wenn (noch) keiner zugeordnet.</summary>
    Task<RouteClusterInfo?> GetClusterForRunAsync(long runId, CancellationToken ct = default);
}
