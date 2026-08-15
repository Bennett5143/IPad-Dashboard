namespace Dashboard.Domain.Running;

/// <summary>
/// Persistenz der Lauf-Orte. Die Zuordnung pflegt der Hintergrund-Sync, die Lese-Methoden speisen
/// Übersicht und Heatmap. Der Vergleich selbst liegt in <see cref="RunPlaceMatcher"/>.
/// </summary>
public interface IRunPlaceStore
{
    /// <summary>Alle Orte als Kandidaten für die Zuordnung (Mittelpunkte).</summary>
    Task<IReadOnlyList<RunPlaceCandidate>> GetCandidatesAsync(CancellationToken ct = default);

    /// <summary>Ids noch nicht zugeordneter Läufe mit Strecke, älteste zuerst (deterministisch).</summary>
    Task<IReadOnlyList<long>> GetUnassignedRunIdsAsync(int limit, CancellationToken ct = default);

    /// <summary>Legt einen Ort mit diesem Lauf an und gibt die Id zurück.</summary>
    Task<int> CreatePlaceAsync(
        long runId, IReadOnlyList<GeoPoint> track, DateTimeOffset whenUtc, CancellationToken ct = default);

    /// <summary>Ordnet einen Lauf einem Ort zu; Mittelpunkt und Ausdehnung wachsen mit.</summary>
    Task AssignAsync(
        long runId, int placeId, IReadOnlyList<GeoPoint> track, DateTimeOffset whenUtc,
        CancellationToken ct = default);

    /// <summary>Markiert einen Lauf ohne nutzbare Strecke als bearbeitet (kein erneuter Versuch).</summary>
    Task MarkUnassignableAsync(long runId, DateTimeOffset whenUtc, CancellationToken ct = default);

    /// <summary>Orts-Übersicht, absteigend nach Laufzahl.</summary>
    Task<IReadOnlyList<RunPlaceSummary>> GetSummariesAsync(CancellationToken ct = default);

    /// <summary>Alle Orte mit Mittelpunkt und Ausdehnung — für Heatmap und Kachel-Warmup.</summary>
    Task<IReadOnlyList<RunPlace>> GetPlacesAsync(CancellationToken ct = default);

    /// <summary>Ort eines Laufs; <c>null</c>, wenn (noch) keiner zugeordnet.</summary>
    Task<RunPlaceInfo?> GetPlaceForRunAsync(long runId, CancellationToken ct = default);

    /// <summary>Lauf-Ids eines Ortes — für die auf den Ort gerahmte Heatmap.</summary>
    Task<IReadOnlyList<long>> GetRunIdsForPlaceAsync(int placeId, CancellationToken ct = default);

    /// <summary>Benennt einen Ort um. Der Name überlebt neue Läufe und erneute Zuordnung.</summary>
    Task RenameAsync(int placeId, string name, CancellationToken ct = default);
}
