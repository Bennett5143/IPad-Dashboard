namespace Dashboard.Domain.Whoop;

/// <summary>Einschlaf-Konsistenz: mittlere Einschlafzeit (Berlin) + Streuung.</summary>
public sealed record BedtimeStats(TimeOnly AverageBedtime, TimeSpan StandardDeviation, int SampleCount);

/// <summary>Aggregat eines Schlaf-Buckets (Einschlaf-Fenster oder Dauer): Stichprobe + Ø-Wert.</summary>
public sealed record SleepBucketStats(string Label, int SampleCount, double? Average);

/// <summary>Schlaf-Performance der Nächte nach Abendtraining vs. aller übrigen Nächte.</summary>
public sealed record EveningTrainingImpact(
    int EveningNights,
    double AvgSleepPerformanceAfterEvening,
    int OtherNights,
    double AvgSleepPerformanceOther);

/// <summary>
/// Schlafenszeiten-Analysen (FA-10.03) auf der persistierten Tageshistorie — reine, testbare
/// Logik. Alle Aussagen sind **Heuristiken** (FA-10.02): Recovery wird von WHOOP aus genau dem
/// Schlaf berechnet, der demselben Kalendertag zugeordnet ist, daher korrelieren die Buckets
/// innerhalb eines Datensatzes.
/// </summary>
public static class SleepAnalyzer
{
    /// <summary>Unterhalb dieser Stichprobe pro Bucket gibt es keine Aussage (FA-10.02).</summary>
    public const int MinSampleForVerdict = 5;

    /// <summary>Ab dieser Berliner End-Stunde zählt ein Workout als Abendtraining.</summary>
    public const int EveningHour = 19;

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    // Anker für die Mittelung der Einschlafzeiten: Bedtimes liegen realistisch zwischen
    // 18:00 und 06:00 — relativ zu 18:00 gemessen gibt es keinen Mitternachts-Umbruch
    // (23:30 und 00:30 mitteln sich sonst zu 12:00 mittags).
    private static readonly TimeSpan BedtimeAnchor = TimeSpan.FromHours(18);

    /// <summary>Mittlere Einschlafzeit + Standardabweichung; <c>null</c> unter 2 Nächten.</summary>
    public static BedtimeStats? AnalyzeBedtimeConsistency(IEnumerable<WhoopDailyMetric> metrics)
    {
        var minutes = metrics
            .Where(m => m.SleepStartUtc is not null)
            .Select(m => MinutesSinceAnchor(m.SleepStartUtc!.Value))
            .ToList();
        if (minutes.Count < 2)
        {
            return null;
        }

        var mean = minutes.Average();
        var variance = minutes.Average(v => (v - mean) * (v - mean));
        var average = TimeOnly.FromTimeSpan(
            (BedtimeAnchor + TimeSpan.FromMinutes(mean)) is var t && t.TotalHours >= 24
                ? t - TimeSpan.FromHours(24)
                : t);

        return new BedtimeStats(average, TimeSpan.FromMinutes(Math.Sqrt(variance)), minutes.Count);
    }

    /// <summary>Ø-Recovery je Einschlaf-Fenster (Berlin): vor 22:30 / 22:30–23:30 / 23:30–00:30 / nach 00:30.</summary>
    public static IReadOnlyList<SleepBucketStats> AnalyzeBedtimeVsRecovery(IEnumerable<WhoopDailyMetric> metrics)
    {
        var labels = new[] { "vor 22:30", "22:30–23:30", "23:30–00:30", "nach 00:30" };
        return Bucketize(
            metrics.Where(m => m is { SleepStartUtc: not null, RecoveryScore: not null }),
            labels,
            m => MinutesSinceAnchor(m.SleepStartUtc!.Value) switch
            {
                < 270 => 0,        // bis 22:30 (270 min nach 18:00)
                < 330 => 1,        // bis 23:30
                < 390 => 2,        // bis 00:30
                _ => 3
            },
            m => m.RecoveryScore!.Value);
    }

    /// <summary>Ø-Recovery je Schlafdauer-Bucket: &lt; 6 h / 6–7 h / 7–8 h / &gt; 8 h.</summary>
    public static IReadOnlyList<SleepBucketStats> AnalyzeDurationVsRecovery(IEnumerable<WhoopDailyMetric> metrics)
    {
        var labels = new[] { "< 6 h", "6–7 h", "7–8 h", "> 8 h" };
        return Bucketize(
            metrics.Where(m => m is { SleepHours: not null, RecoveryScore: not null }),
            labels,
            m => m.SleepHours!.Value switch
            {
                < 6 => 0,
                < 7 => 1,
                <= 8 => 2,
                _ => 3
            },
            m => m.RecoveryScore!.Value);
    }

    /// <summary>
    /// Schlaf-Performance der Nächte nach Abendtraining (Workout-Ende ≥ 19 Uhr Berlin am
    /// Vortag) gegen alle übrigen Nächte; <c>null</c>, solange eine der Gruppen leer ist.
    /// </summary>
    public static EveningTrainingImpact? AnalyzeEveningTraining(
        IEnumerable<WhoopDailyMetric> metrics, IEnumerable<WhoopWorkout> workouts)
    {
        var eveningDays = workouts
            .Select(w => TimeZoneInfo.ConvertTime(w.EndUtc, BerlinTz))
            .Where(local => local.Hour >= EveningHour)
            .Select(local => DateOnly.FromDateTime(local.DateTime))
            .ToHashSet();

        var evening = new List<double>();
        var other = new List<double>();
        foreach (var metric in metrics.Where(m => m.SleepPerformance is not null))
        {
            // Die Nacht, die Tag X zugeordnet ist, folgt auf den Abend von Tag X−1.
            var target = eveningDays.Contains(metric.Date.AddDays(-1)) ? evening : other;
            target.Add(metric.SleepPerformance!.Value);
        }

        return evening.Count == 0 || other.Count == 0
            ? null
            : new EveningTrainingImpact(evening.Count, evening.Average(), other.Count, other.Average());
    }

    /// <summary>Bestes Bucket — nur mit ausreichender Stichprobe (höherer Ø gewinnt); sonst <c>null</c>.</summary>
    public static SleepBucketStats? BestBucket(
        IReadOnlyList<SleepBucketStats> stats, int minSample = MinSampleForVerdict)
    {
        var eligible = stats.Where(s => s.SampleCount >= minSample && s.Average is not null).ToList();
        return eligible.Count == 0 ? null : eligible.OrderByDescending(s => s.Average!.Value).First();
    }

    private static double MinutesSinceAnchor(DateTimeOffset sleepStartUtc)
    {
        var local = TimeZoneInfo.ConvertTime(sleepStartUtc, BerlinTz);
        var sinceAnchor = local.TimeOfDay - BedtimeAnchor;
        if (sinceAnchor < TimeSpan.Zero)
        {
            sinceAnchor += TimeSpan.FromHours(24);
        }

        return sinceAnchor.TotalMinutes;
    }

    private static List<SleepBucketStats> Bucketize(
        IEnumerable<WhoopDailyMetric> metrics,
        string[] labels,
        Func<WhoopDailyMetric, int> bucketSelector,
        Func<WhoopDailyMetric, double> valueSelector)
    {
        var grouped = metrics
            .GroupBy(bucketSelector)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Avg: g.Average(valueSelector)));

        return labels
            .Select((label, index) => grouped.TryGetValue(index, out var agg)
                ? new SleepBucketStats(label, agg.Count, agg.Avg)
                : new SleepBucketStats(label, 0, null))
            .ToList();
    }
}
