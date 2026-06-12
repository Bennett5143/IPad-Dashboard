namespace Dashboard.Domain.Running;

/// <summary>Persistierter Sync-Zustand: letzter erfolgreicher Sync und ggf. letzter Fehler (FA-8.09).</summary>
public sealed record SyncSnapshot(
    DateTimeOffset? LastSuccessfulSyncUtc,
    DateTimeOffset? LastAttemptUtc,
    string? LastError,
    DateTimeOffset? DetailsBackfilledUtc = null);

/// <summary>UI-Sicht auf den Gesamtzustand: verbunden + Sync-Stand.</summary>
public sealed record RunningSyncStatus(
    bool IsConnected,
    DateTimeOffset? LastSuccessfulSyncUtc,
    string? LastError);
