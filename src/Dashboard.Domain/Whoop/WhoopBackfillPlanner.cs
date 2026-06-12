namespace Dashboard.Domain.Whoop;

/// <summary>Ein rückwärts gerichtetes Abruf-Fenster für den historischen WHOOP-Backfill.</summary>
public sealed record WhoopBackfillWindow(DateTimeOffset FromUtc, DateTimeOffset ToUtc);

/// <summary>
/// Plant den gefensterten historischen Backfill der WHOOP-Tageswerte (FA-9.10). Die WHOOP-API
/// deckelt paginierte Abrufe (8 Seiten × 25 Records), deshalb wird die Historie in begrenzten
/// Fenstern rückwärts geholt — höchstens eines pro Sync-Zyklus (drosselt die API-Last).
/// Reine, testbare Logik; den Fortschritts-Cursor hält der aufrufende Dienst.
/// </summary>
public static class WhoopBackfillPlanner
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    /// <summary>
    /// Das nächste Fenster unmittelbar vor <paramref name="backfillBeforeUtc"/>;
    /// <c>null</c>, wenn die konfigurierte Tiefe erreicht ist oder der Backfill
    /// deaktiviert wurde (<paramref name="backfillDays"/> bzw. <paramref name="windowDays"/> ≤ 0).
    /// </summary>
    public static WhoopBackfillWindow? NextWindow(
        DateTimeOffset nowUtc, DateTimeOffset backfillBeforeUtc, int backfillDays, int windowDays)
    {
        if (backfillDays <= 0 || windowDays <= 0)
        {
            return null;
        }

        var floor = nowUtc.AddDays(-backfillDays);
        if (backfillBeforeUtc <= floor)
        {
            return null;
        }

        var from = backfillBeforeUtc.AddDays(-windowDays);
        if (from < floor)
        {
            from = floor;
        }

        return new WhoopBackfillWindow(from, backfillBeforeUtc);
    }

    /// <summary>
    /// Beginn des Berliner Kalendertags als Zeitpunkt mit korrektem UTC-Offset (CET/CEST) —
    /// der Start-Cursor des Backfills vor dem ältesten gespeicherten Tag.
    /// </summary>
    public static DateTimeOffset StartOfBerlinDay(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, BerlinTz.GetUtcOffset(local));
    }
}
