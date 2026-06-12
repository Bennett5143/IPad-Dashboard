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

/// <summary>Schlafphasen-Zusammensetzung der jüngsten Nacht (gestapelter Balken, FA-9.11).</summary>
public sealed record WhoopSleepNight(
    string DateLabel,
    string? TimeRange,
    string AsleepLabel,
    string? RespiratoryLabel,
    IReadOnlyList<WhoopSleepStageSegment> Segments);

/// <summary>Ein Segment des Schlafphasen-Balkens; <see cref="WidthPercent"/> ist der Zeitanteil (0..100).</summary>
public sealed record WhoopSleepStageSegment(string Label, string CssClass, double WidthPercent, string Detail);

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
        Card("Atemfrequenz", "/min", "resp", history, m => m.RespiratoryRate, 1),
    ];

    /// <summary>
    /// Baut den Schlafphasen-Balken der jüngsten Nacht mit Phasen-Daten; <c>null</c>, wenn der
    /// Zeitraum keine enthält. Das Wach-Segment erscheint nur, wenn WHOOP Wachzeit geliefert hat.
    /// </summary>
    public static WhoopSleepNight? BuildSleepNight(IReadOnlyList<WhoopDailyMetric> history)
    {
        var night = history
            .Where(m => m is { LightSleepHours: not null, DeepSleepHours: not null, RemSleepHours: not null })
            .MaxBy(m => m.Date);
        if (night is null)
        {
            return null;
        }

        var stages = new List<(string Label, string Css, double Hours)>
        {
            ("Leicht", "sleep-light", night.LightSleepHours!.Value),
            ("Tief", "sleep-deep", night.DeepSleepHours!.Value),
            ("REM", "sleep-rem", night.RemSleepHours!.Value),
        };
        if (night.AwakeHours is { } awakeHours)
        {
            stages.Add(("Wach", "sleep-awake", awakeHours));
        }

        var total = stages.Sum(s => s.Hours);
        if (total <= 0)
        {
            return null;
        }

        var segments = stages
            .Select(s =>
            {
                var share = s.Hours / total * 100;
                return new WhoopSleepStageSegment(
                    s.Label, s.Css, share,
                    $"{s.Hours.ToString("0.0", German)} h · {Math.Round(share)} %");
            })
            .ToList();

        var asleep = night.LightSleepHours.Value + night.DeepSleepHours.Value + night.RemSleepHours.Value;
        return new WhoopSleepNight(
            DateLabel: night.Date.ToString("dd.MM.", German),
            TimeRange: TimeRangeLabel(night.SleepStartUtc, night.SleepEndUtc),
            AsleepLabel: $"{asleep.ToString("0.0", German)} h Schlaf",
            RespiratoryLabel: night.RespiratoryRate is { } rate
                ? $"Ø {rate.ToString("0.0", German)} Atemzüge/min"
                : null,
            Segments: segments);
    }

    private static string? TimeRangeLabel(DateTimeOffset? startUtc, DateTimeOffset? endUtc)
    {
        if (startUtc is not { } start || endUtc is not { } end)
        {
            return null;
        }

        var from = TimeZoneInfo.ConvertTime(start, BerlinTz);
        var to = TimeZoneInfo.ConvertTime(end, BerlinTz);
        return $"{from.ToString("HH:mm", German)}–{to.ToString("HH:mm", German)}";
    }

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
            if (workout.AverageHeartRate is { } heartRate)
            {
                detail += $" · Ø {heartRate} bpm";
            }
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
