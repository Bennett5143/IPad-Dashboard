using Dashboard.Domain.Whoop;

namespace Dashboard.Tests.Whoop;

public class TimeOfDayAnalyzerTests
{
    private static WhoopWorkout Workout(
        string sport = "running",
        int startHourUtc = 5, // 07:00 Berlin (CEST) → früh
        int day = 1,
        int minutes = 30,
        double? distanceM = 5000,
        int? avgHr = 150,
        double? kilojoule = null,
        double highShare = 0) =>
        new(
            $"{sport}-{day}-{startHourUtc}",
            sport,
            new DateTimeOffset(2026, 6, day, startHourUtc, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, day, startHourUtc, minutes, 0, TimeSpan.Zero),
            distanceM, highShare,
            Kilojoule: kilojoule,
            AverageHeartRate: avgHr);

    [Fact]
    public void BucketFor_UsesBerlinLocalTime()
    {
        // 05:00 UTC = 07:00 Berlin (CEST, Juni) → früh
        Assert.Equal(TimeOfDayBucket.EarlyMorning,
            TimeOfDayAnalyzer.BucketFor(new DateTimeOffset(2026, 6, 1, 5, 0, 0, TimeSpan.Zero)));
        // 17:00 UTC = 19:00 Berlin → abends
        Assert.Equal(TimeOfDayBucket.Evening,
            TimeOfDayAnalyzer.BucketFor(new DateTimeOffset(2026, 6, 1, 17, 0, 0, TimeSpan.Zero)));
        // 22:00 UTC = 00:00 Berlin (Folgetag) → nachts
        Assert.Equal(TimeOfDayBucket.Night,
            TimeOfDayAnalyzer.BucketFor(new DateTimeOffset(2026, 6, 1, 22, 0, 0, TimeSpan.Zero)));
        // Winter (CET, +1): 08:30 UTC = 09:30 Berlin → vormittags
        Assert.Equal(TimeOfDayBucket.Morning,
            TimeOfDayAnalyzer.BucketFor(new DateTimeOffset(2026, 1, 15, 8, 30, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void BeatsPerKm_NeedsHeartRateDistanceAndDuration()
    {
        // 150 bpm × 30 min ÷ 5 km = 900 Schläge/km
        Assert.Equal(900, TimeOfDayAnalyzer.BeatsPerKm(Workout())!.Value, 1);
        Assert.Null(TimeOfDayAnalyzer.BeatsPerKm(Workout(avgHr: null)));
        Assert.Null(TimeOfDayAnalyzer.BeatsPerKm(Workout(distanceM: null)));
    }

    [Fact]
    public void EnergyPerMinute_NeedsKilojoule()
    {
        Assert.Equal(20, TimeOfDayAnalyzer.EnergyPerMinute(Workout(kilojoule: 600))!.Value, 1);
        Assert.Null(TimeOfDayAnalyzer.EnergyPerMinute(Workout(kilojoule: null)));
    }

    [Fact]
    public void CategoryFor_MapsViaHabitKind()
    {
        Assert.Equal(TrainingCategory.Running, TimeOfDayAnalyzer.CategoryFor(Workout()));
        Assert.Equal(TrainingCategory.Strength, TimeOfDayAnalyzer.CategoryFor(Workout(sport: "weightlifting")));
        Assert.Equal(TrainingCategory.JumpRope, TimeOfDayAnalyzer.CategoryFor(Workout(sport: "jumping rope")));
        Assert.Null(TimeOfDayAnalyzer.CategoryFor(Workout(sport: "cycling")));
        Assert.Null(TimeOfDayAnalyzer.CategoryFor(Workout(sport: "yoga"))); // Dehnen bewusst außen vor
    }

    [Fact]
    public void Analyze_GroupsByBucket_CountsOnlyMeasurableWorkouts()
    {
        var workouts = new[]
        {
            Workout(day: 1),                      // früh, 900 Schläge/km
            Workout(day: 2, minutes: 25),         // früh, 750 Schläge/km
            Workout(day: 3, avgHr: null),         // früh, ohne HF → zählt nicht in die Stichprobe
            Workout(day: 4, startHourUtc: 17),    // abends
            Workout(day: 5, sport: "cycling"),    // andere Kategorie → ignoriert
        };

        var stats = TimeOfDayAnalyzer.Analyze(workouts, TrainingCategory.Running);

        Assert.Equal(6, stats.Count); // alle Fenster, auch leere
        var early = stats.Single(s => s.Bucket == TimeOfDayBucket.EarlyMorning);
        Assert.Equal(2, early.SampleCount);
        Assert.Equal(825, early.AverageMeasure!.Value, 1); // (900 + 750) / 2
        Assert.Equal(0, stats.Single(s => s.Bucket == TimeOfDayBucket.Midday).SampleCount);
    }

    [Fact]
    public void BestBucket_RespectsMinSample_AndDirection()
    {
        var stats = new List<TimeOfDayBucketStats>
        {
            new(TimeOfDayBucket.EarlyMorning, 6, 800),  // genug Stichprobe, bester (niedrig)
            new(TimeOfDayBucket.Evening, 6, 900),
            new(TimeOfDayBucket.Midday, 2, 500),        // besserer Wert, aber zu wenig Daten
        };

        Assert.Equal(TimeOfDayBucket.EarlyMorning,
            TimeOfDayAnalyzer.BestBucket(stats, lowerIsBetter: true));
        Assert.Equal(TimeOfDayBucket.Evening,
            TimeOfDayAnalyzer.BestBucket(stats, lowerIsBetter: false));
        Assert.Null(TimeOfDayAnalyzer.BestBucket(
            [new TimeOfDayBucketStats(TimeOfDayBucket.Evening, 4, 900)], lowerIsBetter: true));
    }

    [Fact]
    public void WeekdayMatrix_CountsMondayFirst_BerlinLocal()
    {
        // 01.06.2026 ist ein Montag; 05:00 UTC = früh.
        var matrix = TimeOfDayAnalyzer.WeekdayMatrix(
        [
            Workout(day: 1),                    // Mo früh
            Workout(day: 1, startHourUtc: 17),  // Mo abends
            Workout(day: 7, startHourUtc: 5),   // So (07.06.) früh
            Workout(day: 2, sport: "cycling"),  // keine Kategorie → ignoriert
        ]);

        Assert.Equal(1, matrix[(int)TimeOfDayBucket.EarlyMorning, 0]); // Mo früh
        Assert.Equal(1, matrix[(int)TimeOfDayBucket.Evening, 0]);      // Mo abends
        Assert.Equal(1, matrix[(int)TimeOfDayBucket.EarlyMorning, 6]); // So früh
        Assert.Equal(0, matrix[(int)TimeOfDayBucket.Midday, 2]);
    }
}
