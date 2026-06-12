namespace Dashboard.Domain.Whoop;

/// <summary>
/// Persistenz der WHOOP-Tageswerte (FA-9.10): ein Datensatz pro Kalendertag (Berlin),
/// fortlaufend vom Hintergrund-Sync befüllt. Quelle für Verlaufs-Auswertungen, ohne je
/// Seitenaufruf die WHOOP-API abfragen zu müssen.
/// </summary>
public interface IWhoopMetricStore
{
    /// <summary>
    /// Legt die Tageswerte an bzw. aktualisiert sie. Feldweise: Ein vorhandener Wert wird
    /// überschrieben (WHOOP re-scored Tage nachträglich), <c>null</c> lässt Bestehendes
    /// stehen — ein Abruf-Fenster, das einen Tag nur anschneidet, darf keine Daten löschen.
    /// </summary>
    Task UpsertAsync(IReadOnlyList<WhoopDailyMetric> metrics, CancellationToken ct = default);

    /// <summary>Tageswerte im Bereich (beide Grenzen inklusiv), aufsteigend nach Datum.</summary>
    Task<IReadOnlyList<WhoopDailyMetric>> GetRangeAsync(
        DateOnly fromInclusive, DateOnly toInclusive, CancellationToken ct = default);

    /// <summary>Ältester gespeicherter Tag; <c>null</c>, solange der Store leer ist.</summary>
    Task<DateOnly?> GetOldestDateAsync(CancellationToken ct = default);
}
