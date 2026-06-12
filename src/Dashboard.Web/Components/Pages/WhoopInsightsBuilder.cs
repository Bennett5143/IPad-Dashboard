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

/// <summary>Eine Trainingsart-Karte der Tageszeit-Auswertung (FA-10.01).</summary>
public sealed record TimeOfDayCard(
    string Title,
    string MeasureHint,
    string Verdict,
    IReadOnlyList<TimeOfDayRow> Rows);

/// <summary>Ein Zeitfenster innerhalb einer Trainingsart-Karte.</summary>
public sealed record TimeOfDayRow(
    string BucketLabel, int Count, string ValueLabel, double BarPercent, bool IsBest, bool LowSample);

/// <summary>Ein Schlaf-Bucket (Einschlaf-Fenster oder Dauer) mit Ø-Recovery (FA-10.03).</summary>
public sealed record SleepBucketRow(
    string Label, int Count, string ValueLabel, double BarPercent, bool IsBest, bool LowSample);

/// <summary>View-Modell der Schlafenszeiten-Sektion; Teile fehlen, wenn die Daten fehlen.</summary>
public sealed record SleepInsightsView(
    string? ConsistencyLabel,
    IReadOnlyList<SleepBucketRow> BedtimeRows,
    string BedtimeVerdict,
    IReadOnlyList<SleepBucketRow> DurationRows,
    string DurationVerdict,
    string? EveningLabel);

/// <summary>Trainingslast-Anzeige (FA-10.04): aktueller ACWR + Zone + Verlaufs-Sparkline.</summary>
public sealed record TrainingLoadView(
    string RatioLabel,
    string ZoneLabel,
    string ZoneCss,
    IReadOnlyList<double?> Sparkline,
    string? ConfidenceHint,
    string MethodHint);

/// <summary>Aerobe Fitness-Kurve (FA-10.05): Monats-Ø Herzschläge/km + Trend vs. ~3 Monate zuvor.</summary>
public sealed record FitnessCurveView(
    IReadOnlyList<double?> Sparkline,
    string CurrentLabel,
    string? TrendLabel,
    string? TrendCss,
    string Hint);

/// <summary>Trainings-Häufigkeit als Matrix Zeitfenster × Wochentag.</summary>
public sealed record TimeOfDayMatrix(IReadOnlyList<string> DayLabels, IReadOnlyList<TimeOfDayMatrixRow> Rows);

public sealed record TimeOfDayMatrixRow(string BucketLabel, IReadOnlyList<TimeOfDayMatrixCell> Cells);

public sealed record TimeOfDayMatrixCell(int Count, string Css);

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

    private static readonly string[] BucketLabels =
        ["früh", "vormittags", "mittags", "nachmittags", "abends", "nachts"];

    private static readonly string[] DayLabels = ["Mo", "Di", "Mi", "Do", "Fr", "Sa", "So"];

    /// <summary>
    /// Baut die Tageszeit-Karten (FA-10.01); Trainingsarten ganz ohne messbare Workouts
    /// entfallen. Stichproben stehen an jeder Zeile, die Bestzeit-Aussage kommt aus dem
    /// Analyzer (mind. 5 Trainings je Fenster, FA-10.02).
    /// </summary>
    public static IReadOnlyList<TimeOfDayCard> BuildTimeOfDayCards(IReadOnlyList<WhoopWorkout> workouts)
    {
        List<TimeOfDayCard> cards = [];
        Add(TrainingCategory.Running, "Laufen", "Ø Herzschläge/km · niedriger = effizienter", "Schläge/km", "0");
        Add(TrainingCategory.Strength, "Kraft", "Ø kJ/min · höher = dichter", "kJ/min", "0.0");
        Add(TrainingCategory.JumpRope, "Seilspringen", "Ø kJ/min · höher = dichter", "kJ/min", "0.0");
        return cards;

        void Add(TrainingCategory category, string title, string hint, string unit, string format)
        {
            var stats = TimeOfDayAnalyzer.Analyze(workouts, category);
            if (stats.Sum(s => s.SampleCount) == 0)
            {
                return;
            }

            var best = TimeOfDayAnalyzer.BestBucket(stats, TimeOfDayAnalyzer.LowerIsBetter(category));
            var maxMeasure = stats.Max(s => s.AverageMeasure) ?? 0;

            var rows = stats
                .Select(s => new TimeOfDayRow(
                    BucketLabels[(int)s.Bucket],
                    s.SampleCount,
                    s.AverageMeasure is { } value ? value.ToString(format, German) : "–",
                    maxMeasure > 0 && s.AverageMeasure is { } v ? v / maxMeasure * 100 : 0,
                    IsBest: best == s.Bucket,
                    LowSample: s.SampleCount is > 0 and < TimeOfDayAnalyzer.MinSampleForVerdict))
                .ToList();

            var verdict = best is { } bucket
                ? FormatVerdict(stats.Single(s => s.Bucket == bucket), unit, format)
                : $"Noch keine belastbare Aussage – mind. {TimeOfDayAnalyzer.MinSampleForVerdict} Trainings je Zeitfenster nötig.";

            cards.Add(new TimeOfDayCard(title, hint, verdict, rows));
        }

        string FormatVerdict(TimeOfDayBucketStats best, string unit, string format) =>
            $"Stärkstes Zeitfenster: {BucketLabels[(int)best.Bucket]} – " +
            $"Ø {best.AverageMeasure!.Value.ToString(format, German)} {unit} (n = {best.SampleCount})";
    }

    /// <summary>
    /// Baut die Schlafenszeiten-Sektion (FA-10.03); <c>null</c>, solange die Historie keine
    /// Schlafdaten enthält. Aussagen erst ab 5 Nächten pro Bucket (FA-10.02).
    /// </summary>
    public static SleepInsightsView? BuildSleepInsights(
        IReadOnlyList<WhoopDailyMetric> metrics, IReadOnlyList<WhoopWorkout> workouts)
    {
        var consistency = SleepAnalyzer.AnalyzeBedtimeConsistency(metrics);
        var bedtime = SleepAnalyzer.AnalyzeBedtimeVsRecovery(metrics);
        var duration = SleepAnalyzer.AnalyzeDurationVsRecovery(metrics);
        var evening = SleepAnalyzer.AnalyzeEveningTraining(metrics, workouts);

        if (consistency is null
            && bedtime.Sum(b => b.SampleCount) == 0
            && duration.Sum(d => d.SampleCount) == 0)
        {
            return null;
        }

        return new SleepInsightsView(
            consistency is { } c
                ? $"Ø Einschlafzeit {c.AverageBedtime.ToString("HH:mm", German)} ± " +
                  $"{(int)Math.Round(c.StandardDeviation.TotalMinutes)} min (n = {c.SampleCount})"
                : null,
            SleepRows(bedtime),
            SleepVerdict(bedtime),
            SleepRows(duration),
            SleepVerdict(duration),
            evening is { } e
                ? $"Nach Abendtraining (Ende ≥ {SleepAnalyzer.EveningHour} Uhr): Ø Schlaf-Performance " +
                  $"{e.AvgSleepPerformanceAfterEvening.ToString("0", German)} % (n = {e.EveningNights}) – " +
                  $"sonst {e.AvgSleepPerformanceOther.ToString("0", German)} % (n = {e.OtherNights})."
                : null);
    }

    /// <summary>
    /// Baut die Trainingslast-Anzeige (FA-10.04); <c>null</c>, solange die chronische EWMA
    /// noch im Warmlauf ist (mind. 28 Tage Strain-Historie nötig).
    /// </summary>
    public static TrainingLoadView? BuildTrainingLoad(IReadOnlyList<WhoopDailyMetric> metrics)
    {
        var points = TrainingLoadCalculator.Compute(metrics);
        if (points.Count == 0 || points[^1].Ratio is not { } ratio)
        {
            return null;
        }

        var zone = TrainingLoadCalculator.ZoneFor(ratio);
        var acuteDays = TrainingLoadCalculator.AcuteDaysWithData(metrics, points[^1].Date);

        return new TrainingLoadView(
            ratio.ToString("0.00", German),
            zone switch
            {
                TrainingLoadZone.Low => "Unterlast",
                TrainingLoadZone.Balanced => "ausgewogen",
                TrainingLoadZone.Elevated => "erhöht",
                _ => "hoch"
            },
            zone switch
            {
                TrainingLoadZone.Low => "load-low",
                TrainingLoadZone.Balanced => "load-ok",
                TrainingLoadZone.Elevated => "load-warn",
                _ => "load-high"
            },
            points.TakeLast(90).Select(p => p.Ratio).ToList(),
            acuteDays < TrainingLoadCalculator.MinAcuteSamples
                ? $"Nur {acuteDays} von {TrainingLoadCalculator.AcuteDays} Tagen mit Daten – Aussage eingeschränkt."
                : null,
            $"Akut ({TrainingLoadCalculator.AcuteDays} Tage) ÷ chronisch ({TrainingLoadCalculator.ChronicDays} Tage), " +
            "EWMA über den Tages-Strain – Form-Heuristik, keine Verletzungs-Vorhersage.");
    }

    /// <summary>
    /// Baut die Fitness-Kurve (FA-10.05) aus den Lauf-Metriken; <c>null</c>, solange kein
    /// Monat die Min-Stichprobe erreicht. Trend nur, wenn ~3 Monate zuvor vergleichbar sind.
    /// </summary>
    public static FitnessCurveView? BuildFitnessCurve(IReadOnlyList<Run> runs)
    {
        var months = AerobicEfficiencyCalculator.Monthly(runs);
        var latest = months.LastOrDefault(m => m.AvgBeatsPerKm is not null);
        if (latest is null)
        {
            return null;
        }

        var trend = AerobicEfficiencyCalculator.TrendPercent(months);
        string? trendLabel = null, trendCss = null;
        if (trend is { } t)
        {
            var percent = Math.Abs(t).ToString("0.0", German);
            (trendLabel, trendCss) = Math.Abs(t) < 0.5
                ? ("stabil gegenüber vor ~3 Monaten", "trend-flat")
                : t < 0
                    ? ($"{percent} % effizienter als vor ~3 Monaten", "trend-good")
                    : ($"{percent} % weniger effizient als vor ~3 Monaten", "trend-bad");
        }

        return new FitnessCurveView(
            months.TakeLast(12).Select(m => m.AvgBeatsPerKm).ToList(),
            $"Ø {latest.AvgBeatsPerKm!.Value.ToString("0", German)} Schläge/km (n = {latest.SampleCount})",
            trendLabel,
            trendCss,
            $"Monats-Ø der Herzschläge pro km über alle Läufe ≥ {AerobicEfficiencyCalculator.MinDistanceKm:0} km – " +
            $"niedriger = aerob effizienter; Monate mit < {AerobicEfficiencyCalculator.MinRunsPerMonth} Läufen bleiben leer. Heuristik.");
    }

    private static IReadOnlyList<SleepBucketRow> SleepRows(IReadOnlyList<SleepBucketStats> stats)
    {
        var best = SleepAnalyzer.BestBucket(stats);
        var max = stats.Max(s => s.Average) ?? 0;

        return stats
            .Select(s => new SleepBucketRow(
                s.Label,
                s.SampleCount,
                s.Average is { } avg ? $"{avg.ToString("0", German)} %" : "–",
                max > 0 && s.Average is { } value ? value / max * 100 : 0,
                IsBest: best is not null && s.Label == best.Label,
                LowSample: s.SampleCount is > 0 and < SleepAnalyzer.MinSampleForVerdict))
            .ToList();
    }

    private static string SleepVerdict(IReadOnlyList<SleepBucketStats> stats) =>
        SleepAnalyzer.BestBucket(stats) is { } best
            ? $"Beste Ø-Recovery: {best.Label} – {best.Average!.Value.ToString("0", German)} % (n = {best.SampleCount})"
            : $"Noch keine belastbare Aussage – mind. {SleepAnalyzer.MinSampleForVerdict} Nächte je Bucket nötig.";

    /// <summary>Trainings-Häufigkeit Zeitfenster × Wochentag (alle Trainingsarten zusammen).</summary>
    public static TimeOfDayMatrix BuildTimeOfDayMatrix(IReadOnlyList<WhoopWorkout> workouts)
    {
        var matrix = TimeOfDayAnalyzer.WeekdayMatrix(workouts);

        var rows = Enum.GetValues<TimeOfDayBucket>()
            .Select(bucket => new TimeOfDayMatrixRow(
                BucketLabels[(int)bucket],
                Enumerable.Range(0, 7)
                    .Select(day => Cell(matrix[(int)bucket, day]))
                    .ToList()))
            .ToList();

        return new TimeOfDayMatrix(DayLabels, rows);

        static TimeOfDayMatrixCell Cell(int count) => new(count, count switch
        {
            0 => "cell-0",
            <= 2 => "cell-1",
            <= 5 => "cell-2",
            _ => "cell-3"
        });
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
