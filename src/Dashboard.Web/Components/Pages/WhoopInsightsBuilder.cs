using System.Globalization;

using Dashboard.Web.Components.Tiles;

namespace Dashboard.Web.Components.Pages;

/// <summary>Metrik-Karte der Insights-Seite (Sparkline-Werte + formatierte Kennzahlen).</summary>
public sealed record WhoopMetricCard(
    string Title,
    string Unit,
    string CssClass,
    IReadOnlyList<double?> Values,
    string Current,
    string Avg,
    string Min,
    string Max);

/// <summary>Eine Lauf-Zeile der „Läufe nach Recovery"-Liste.</summary>
public sealed record WhoopRunRow(string Date, string Kind, string Detail, string RecoveryCss);

/// <summary>
/// Baut die View-Modelle der WHOOP-Insights-Seite aus Tages-Historie und Workouts –
/// reine, testbare Aufbereitung ohne Blazor-Abhängigkeiten.
/// </summary>
public static class WhoopInsightsBuilder
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static IReadOnlyList<WhoopMetricCard> BuildCards(IReadOnlyList<WhoopDailyMetric> history) =>
    [
        Card("Recovery", "%", "recovery", history, m => m.RecoveryScore, 0),
        Card("HRV", "ms", "hrv", history, m => m.HrvMillis, 0),
        Card("Ruhepuls", "bpm", "rhr", history, m => m.RestingHeartRate, 0),
        Card("Schlaf", "h", "sleep", history, m => m.SleepHours, 1),
        Card("Tages-Strain", "", "strain", history, m => m.DayStrain, 1),
    ];

    public static IReadOnlyList<WhoopRunRow> BuildRuns(
        IReadOnlyList<WhoopWorkout> workouts, IReadOnlyList<WhoopDailyMetric> history)
    {
        var recoveryByDate = history
            .Where(m => m.RecoveryLevel is not null)
            .ToDictionary(m => m.Date, m => m.RecoveryLevel);

        var runs = new List<WhoopRunRow>();
        foreach (var workout in workouts.OrderByDescending(w => w.StartUtc))
        {
            var kind = WhoopHabitMapper.MapKind(workout);
            if (kind is not (HabitKind.Zone2Run or HabitKind.Vo2MaxIntervals))
            {
                continue;
            }

            var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(workout.StartUtc, BerlinTz).DateTime);
            var details = WhoopHabitMapper.BuildRunningDetails(workout);
            var detail = details is null
                ? $"{(int)workout.Duration.TotalMinutes} min"
                : $"{details.DurationMinutes} min · {details.PaceMinPerKm.ToString("0.00", German)} min/km";
            var label = kind == HabitKind.Vo2MaxIntervals ? "VO2max" : "Zone 2";

            runs.Add(new WhoopRunRow(
                date.ToString("dd.MM.", German), label, detail, RecoveryCss(recoveryByDate.GetValueOrDefault(date))));
        }

        return runs;
    }

    private static WhoopMetricCard Card(
        string title, string unit, string css, IReadOnlyList<WhoopDailyMetric> history,
        Func<WhoopDailyMetric, double?> selector, int decimals)
    {
        var values = history.Select(selector).ToList();
        var present = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        string Fmt(double v) => v.ToString("F" + decimals, German);

        return new WhoopMetricCard(
            title, unit, css, values,
            Current: present.Count > 0 ? Fmt(present[^1]) : "–",
            Avg: present.Count > 0 ? Fmt(present.Average()) : "–",
            Min: present.Count > 0 ? Fmt(present.Min()) : "–",
            Max: present.Count > 0 ? Fmt(present.Max()) : "–");
    }

    private static string RecoveryCss(WhoopRecoveryLevel? level) =>
        level is null ? "recovery-none" : WhoopFormatter.RecoveryCss(level);
}
