namespace Dashboard.Domain.Running;

/// <summary>Persistenz des Sync-Zustands (letzter Erfolg/Fehler) für Anzeige und inkrementelle Logik.</summary>
public interface ISyncStateStore
{
    Task<SyncSnapshot> GetAsync(CancellationToken ct = default);
    Task RecordSuccessAsync(DateTimeOffset whenUtc, CancellationToken ct = default);
    Task RecordFailureAsync(string error, DateTimeOffset whenUtc, CancellationToken ct = default);

    /// <summary>Markiert den einmaligen Voll-Re-Sync der Aktivitätsmetriken als erledigt (FA-8.14).</summary>
    Task MarkDetailsBackfilledAsync(DateTimeOffset whenUtc, CancellationToken ct = default);
}
