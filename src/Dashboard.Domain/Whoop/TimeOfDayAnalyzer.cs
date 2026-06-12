using Dashboard.Domain.Enums;

namespace Dashboard.Domain.Whoop;

/// <summary>Berliner Tageszeit-Fenster eines Trainingsstarts.</summary>
public enum TimeOfDayBucket
{
    EarlyMorning, // 05–09 Uhr
    Morning,      // 09–12 Uhr
    Midday,       // 12–15 Uhr
    Afternoon,    // 15–18 Uhr
    Evening,      // 18–22 Uhr
    Night         // 22–05 Uhr
}

/// <summary>Trainingsarten der Tageszeit-Auswertung (Dehnen bewusst außen vor — keine Intensitätsmessung).</summary>
public enum TrainingCategory
{
    Running,
    Strength,
    JumpRope
}

/// <summary>Aggregat eines Zeitfensters: Stichprobe + Ø des Effektivitätsmaßes.</summary>
public sealed record TimeOfDayBucketStats(TimeOfDayBucket Bucket, int SampleCount, double? AverageMeasure);

/// <summary>
/// Kern der Tageszeit-Effektivität (FA-10.01): Zu welchen Tageszeiten hole ich am meisten aus
/// meinen Trainings heraus? Die Maße sind bewusst **Heuristiken** (FA-10.02) und je Trainingsart
/// verschieden: Läufe = Herzschläge pro km (niedriger = aerob effizienter), Kraft/Seilspringen =
/// Energie pro Minute (höher = dichter). Reine, testbare Logik ohne Persistenz-Abhängigkeiten.
/// </summary>
public static class TimeOfDayAnalyzer
{
    /// <summary>Unterhalb dieser Stichprobe pro Zeitfenster gibt es keine „Bestzeit"-Aussage (FA-10.02).</summary>
    public const int MinSampleForVerdict = 5;

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static TimeOfDayBucket BucketFor(DateTimeOffset startUtc)
    {
        var hour = TimeZoneInfo.ConvertTime(startUtc, BerlinTz).Hour;
        return hour switch
        {
            >= 5 and < 9 => TimeOfDayBucket.EarlyMorning,
            >= 9 and < 12 => TimeOfDayBucket.Morning,
            >= 12 and < 15 => TimeOfDayBucket.Midday,
            >= 15 and < 18 => TimeOfDayBucket.Afternoon,
            >= 18 and < 22 => TimeOfDayBucket.Evening,
            _ => TimeOfDayBucket.Night
        };
    }

    /// <summary>Trainingsart eines Workouts; <c>null</c> für alles ohne Effektivitätsmaß.</summary>
    public static TrainingCategory? CategoryFor(WhoopWorkout workout) =>
        WhoopHabitMapper.MapKind(workout) switch
        {
            HabitKind.Zone2Run or HabitKind.Vo2MaxIntervals => TrainingCategory.Running,
            HabitKind.Strength => TrainingCategory.Strength,
            HabitKind.JumpRope => TrainingCategory.JumpRope,
            _ => null
        };

    /// <summary>Lauf-Effizienz: Herzschläge pro km (Ø-HF × Minuten ÷ km); niedriger = besser.</summary>
    public static double? BeatsPerKm(WhoopWorkout workout)
    {
        var minutes = workout.Duration.TotalMinutes;
        if (workout.AverageHeartRate is not > 0 || workout.DistanceMeters is not > 0 || minutes <= 0)
        {
            return null;
        }

        return workout.AverageHeartRate.Value * minutes / (workout.DistanceMeters.Value / 1000.0);
    }

    /// <summary>Belastungsdichte: Energie pro Minute (kJ/min); höher = dichter.</summary>
    public static double? EnergyPerMinute(WhoopWorkout workout)
    {
        var minutes = workout.Duration.TotalMinutes;
        if (workout.Kilojoule is not > 0 || minutes <= 0)
        {
            return null;
        }

        return workout.Kilojoule.Value / minutes;
    }

    /// <summary>Das zur Trainingsart passende Maß (siehe Klassen-Doku).</summary>
    public static double? MeasureFor(WhoopWorkout workout, TrainingCategory category) =>
        category == TrainingCategory.Running ? BeatsPerKm(workout) : EnergyPerMinute(workout);

    /// <summary>Bei welchen Maßen ist „kleiner" das Bessere?</summary>
    public static bool LowerIsBetter(TrainingCategory category) => category == TrainingCategory.Running;

    /// <summary>
    /// Aggregiert eine Trainingsart über die Zeitfenster. Die Stichprobe zählt nur Workouts,
    /// die das Maß tatsächlich liefern (z. B. Läufe ohne HF fallen heraus) — n passt damit
    /// immer zum ausgewiesenen Durchschnitt.
    /// </summary>
    public static IReadOnlyList<TimeOfDayBucketStats> Analyze(
        IEnumerable<WhoopWorkout> workouts, TrainingCategory category)
    {
        var byBucket = workouts
            .Where(w => CategoryFor(w) == category)
            .Select(w => (Bucket: BucketFor(w.StartUtc), Measure: MeasureFor(w, category)))
            .Where(x => x.Measure is not null)
            .GroupBy(x => x.Bucket)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Avg: g.Average(x => x.Measure!.Value)));

        return Enum.GetValues<TimeOfDayBucket>()
            .Select(bucket => byBucket.TryGetValue(bucket, out var agg)
                ? new TimeOfDayBucketStats(bucket, agg.Count, agg.Avg)
                : new TimeOfDayBucketStats(bucket, 0, null))
            .ToList();
    }

    /// <summary>Bestes Zeitfenster — nur aus Fenstern mit ausreichender Stichprobe; sonst <c>null</c>.</summary>
    public static TimeOfDayBucket? BestBucket(
        IReadOnlyList<TimeOfDayBucketStats> stats, bool lowerIsBetter, int minSample = MinSampleForVerdict)
    {
        var eligible = stats
            .Where(s => s.SampleCount >= minSample && s.AverageMeasure is not null)
            .ToList();
        if (eligible.Count == 0)
        {
            return null;
        }

        var ordered = eligible.OrderBy(s => s.AverageMeasure!.Value);
        return (lowerIsBetter ? ordered.First() : ordered.Last()).Bucket;
    }

    /// <summary>Trainings-Häufigkeit als Matrix Zeitfenster × Wochentag (Mo = Spalte 0).</summary>
    public static int[,] WeekdayMatrix(IEnumerable<WhoopWorkout> workouts)
    {
        var matrix = new int[Enum.GetValues<TimeOfDayBucket>().Length, 7];
        foreach (var workout in workouts)
        {
            if (CategoryFor(workout) is null)
            {
                continue;
            }

            var local = TimeZoneInfo.ConvertTime(workout.StartUtc, BerlinTz);
            var weekday = ((int)local.DayOfWeek + 6) % 7; // Mo = 0 … So = 6
            matrix[(int)BucketFor(workout.StartUtc), weekday]++;
        }

        return matrix;
    }
}
