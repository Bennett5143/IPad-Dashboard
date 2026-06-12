namespace Dashboard.Web.Components.Pages;

/// <summary>Eine Zeile der Datenquellen-Liste auf <c>/status</c>.</summary>
public sealed record StatusSliceRow(string Label, string StateLabel, string CssClass, string UpdatedLabel);

/// <summary>
/// Baut die View-Modelle der Status-Seite — reine, testbare Aufbereitung ohne
/// Blazor-Abhängigkeiten (Muster <see cref="WhoopInsightsBuilder"/>).
/// </summary>
public static class StatusViewBuilder
{
    private static readonly Dictionary<string, string> GermanLabels = new()
    {
        ["Weather"] = "Wetter",
        ["Football"] = "Fußball",
        ["Hvv"] = "HVV",
        ["Whoop"] = "WHOOP",
    };

    public static IReadOnlyList<StatusSliceRow> BuildSliceRows(
        IEnumerable<(string Name, bool HasData, bool IsStale, DateTimeOffset? LastUpdatedUtc)> sources,
        DateTimeOffset nowUtc) =>
        sources
            .Select(s => new StatusSliceRow(
                GermanLabels.GetValueOrDefault(s.Name, s.Name),
                s switch
                {
                    { HasData: false } => "keine Daten",
                    { IsStale: true } => "veraltet",
                    _ => "aktuell"
                },
                s switch
                {
                    { HasData: false } => "status-idle",
                    { IsStale: true } => "status-warn",
                    _ => "status-ok"
                },
                s.LastUpdatedUtc is { } updated ? RelativeLabel(updated, nowUtc) : "–"))
            .OrderBy(r => r.Label, StringComparer.Ordinal)
            .ToList();

    /// <summary>„gerade eben" / „vor 5 min" / „vor 3 h" / „vor 4 Tagen".</summary>
    public static string RelativeLabel(DateTimeOffset whenUtc, DateTimeOffset nowUtc)
    {
        var age = nowUtc - whenUtc;
        if (age < TimeSpan.Zero)
        {
            age = TimeSpan.Zero;
        }

        return age.TotalSeconds < 60 ? "gerade eben"
            : age.TotalMinutes < 60 ? $"vor {(int)age.TotalMinutes} min"
            : age.TotalHours < 48 ? $"vor {(int)age.TotalHours} h"
            : $"vor {(int)age.TotalDays} Tagen";
    }

    /// <summary>Backfill-Stand einer Historie: wie weit reichen die Daten zurück?</summary>
    public static string HistoryDepthLabel(DateOnly? oldestStored, DateOnly todayBerlin) =>
        oldestStored is { } oldest
            ? $"zurück bis {oldest:dd.MM.yyyy} ({Math.Max(0, todayBerlin.DayNumber - oldest.DayNumber)} Tage)"
            : "wartet auf erste Daten";
}
