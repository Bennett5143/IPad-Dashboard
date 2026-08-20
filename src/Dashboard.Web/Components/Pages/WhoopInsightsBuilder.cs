using System.Globalization;

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

/// <summary>Ein Schlaf-Bucket (Einschlaf-Fenster oder Dauer) mit Ø-Recovery (FA-10.03).</summary>
public sealed record SleepBucketRow(
    string Label, int Count, string ValueLabel, double BarPercent, bool IsBest, bool LowSample);

/// <summary>View-Modell der Schlafenszeiten-Sektion; Teile fehlen, wenn die Daten fehlen.</summary>
public sealed record SleepInsightsView(
    string? ConsistencyLabel,
    IReadOnlyList<SleepBucketRow> BedtimeRows,
    string BedtimeVerdict,
    IReadOnlyList<SleepBucketRow> DurationRows,
    string DurationVerdict);

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

/// <summary>Eine Treiber-Zeile der Recovery-Korrelationen (FA-10.06).</summary>
public sealed record RecoveryDriverRow(
    string Label, string RLabel, string StrengthLabel, double BarPercent, int Count, bool LowSample);

/// <summary>Ein Scatter (Faktor → Recovery) mit Roh-Wertepaaren.</summary>
public sealed record RecoveryScatterView(
    string Title, string AxisHint, IReadOnlyList<(double X, double Y)> Pairs, int Count);

/// <summary>View-Modell der Recovery-Treiber-Sektion.</summary>
public sealed record RecoveryDriversView(
    IReadOnlyList<RecoveryDriverRow> Rows,
    IReadOnlyList<RecoveryScatterView> Scatters,
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

    private static readonly string[] BucketLabels =
        ["früh", "vormittags", "mittags", "nachmittags", "abends", "nachts"];

    private static readonly string[] DayLabels = ["Mo", "Di", "Mi", "Do", "Fr", "Sa", "So"];

    /// <summary>
    /// Baut die Schlafenszeiten-Sektion (FA-10.03); <c>null</c>, solange die Historie keine
    /// Schlafdaten enthält. Aussagen erst ab 5 Nächten pro Bucket (FA-10.02).
    /// </summary>
    public static SleepInsightsView? BuildSleepInsights(IReadOnlyList<WhoopDailyMetric> metrics)
    {
        var consistency = SleepAnalyzer.AnalyzeBedtimeConsistency(metrics);
        var bedtime = SleepAnalyzer.AnalyzeBedtimeVsRecovery(metrics);
        var duration = SleepAnalyzer.AnalyzeDurationVsRecovery(metrics);

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
            SleepVerdict(duration));
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

    /// <summary>
    /// Baut die Recovery-Treiber-Sektion (FA-10.06); <c>null</c>, solange gar keine Paare
    /// vorliegen. Scatter erscheinen erst ab der Korrelations-Mindeststichprobe.
    /// </summary>
    public static RecoveryDriversView? BuildRecoveryDrivers(IReadOnlyList<WhoopDailyMetric> metrics)
    {
        var stats = RecoveryDriverAnalyzer.Analyze(metrics);
        if (stats.All(s => s.SampleCount == 0))
        {
            return null;
        }

        var rows = stats
            .Select(s => new RecoveryDriverRow(
                FactorLabel(s.Factor),
                s.PearsonR is { } r
                    ? (r >= 0 ? "+" : "−") + Math.Abs(r).ToString("0.00", German)
                    : "–",
                s.PearsonR is { } value ? StrengthLabel(value) : "zu wenig Daten",
                s.PearsonR is { } abs ? Math.Abs(abs) * 100 : 0,
                s.SampleCount,
                LowSample: s.SampleCount is > 0 and < RecoveryDriverAnalyzer.MinSamples))
            .ToList();

        List<RecoveryScatterView> scatters = [];
        AddScatter(RecoveryFactor.SleepDuration, "Schlafdauer → Recovery", "Stunden Schlaf → Recovery %");
        AddScatter(RecoveryFactor.PreviousDayStrain, "Vortages-Strain → Recovery", "Strain am Vortag → Recovery %");

        return new RecoveryDriversView(
            rows,
            scatters,
            $"Pearson-Korrelation über die persistierte Historie – Zusammenhang, keine Kausalität; " +
            $"Werte erst ab {RecoveryDriverAnalyzer.MinSamples} Tagen.");

        void AddScatter(RecoveryFactor factor, string title, string axisHint)
        {
            var pairs = RecoveryDriverAnalyzer.Pairs(metrics, factor);
            if (pairs.Count >= RecoveryDriverAnalyzer.MinSamples)
            {
                scatters.Add(new RecoveryScatterView(title, axisHint, pairs, pairs.Count));
            }
        }

        static string FactorLabel(RecoveryFactor factor) => factor switch
        {
            RecoveryFactor.SleepDuration => "Schlafdauer",
            RecoveryFactor.Bedtime => "Einschlafzeit (später)",
            _ => "Vortages-Strain"
        };

        static string StrengthLabel(double r) => Math.Abs(r) switch
        {
            < 0.1 => "kein Zusammenhang",
            < 0.3 => "schwach",
            < 0.5 => "mittel",
            _ => "stark"
        };
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
}
