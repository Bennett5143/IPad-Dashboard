using Dashboard.Domain.Whoop;

namespace Dashboard.Tests.Whoop;

public class SleepAnalyzerTests
{
    private static WhoopDailyMetric Night(
        int day, int? recovery = null, double? sleepHours = null, int? performance = null,
        DateTimeOffset? sleepStartUtc = null) =>
        new(new DateOnly(2026, 6, day), recovery, null, null, sleepHours, performance, null,
            SleepStartUtc: sleepStartUtc);

    private static DateTimeOffset Utc(int day, int hour, int minute = 0) =>
        new(2026, 6, day, hour, minute, 0, TimeSpan.Zero);

    [Fact]
    public void AnalyzeBedtimeConsistency_AveragesAcrossMidnight()
    {
        // 23:30 und 00:30 Berlin (CEST = UTC+2): 21:30 UTC und 22:30 UTC.
        var stats = SleepAnalyzer.AnalyzeBedtimeConsistency(
        [
            Night(10, sleepStartUtc: Utc(9, 21, 30)),
            Night(11, sleepStartUtc: Utc(10, 22, 30)),
        ]);

        Assert.NotNull(stats);
        Assert.Equal(new TimeOnly(0, 0), stats!.AverageBedtime); // Mitternacht, nicht 12:00 mittags
        Assert.Equal(30, stats.StandardDeviation.TotalMinutes, 1);
        Assert.Equal(2, stats.SampleCount);
    }

    [Fact]
    public void AnalyzeBedtimeConsistency_NeedsAtLeastTwoNights()
    {
        Assert.Null(SleepAnalyzer.AnalyzeBedtimeConsistency([Night(10, sleepStartUtc: Utc(9, 21))]));
        Assert.Null(SleepAnalyzer.AnalyzeBedtimeConsistency([Night(10, recovery: 70)])); // ohne Schlafzeit
    }

    [Fact]
    public void AnalyzeBedtimeVsRecovery_BucketsBerlinBedtimes()
    {
        var stats = SleepAnalyzer.AnalyzeBedtimeVsRecovery(
        [
            Night(10, recovery: 80, sleepStartUtc: Utc(9, 19, 45)),  // 21:45 Berlin → vor 22:30
            Night(11, recovery: 60, sleepStartUtc: Utc(10, 21, 0)),  // 23:00 → 22:30–23:30
            Night(12, recovery: 40, sleepStartUtc: Utc(11, 23, 30)), // 01:30 → nach 00:30
            Night(13, sleepStartUtc: Utc(12, 21, 0)),                // ohne Recovery → zählt nicht
        ]);

        Assert.Equal(4, stats.Count);
        Assert.Equal(1, stats[0].SampleCount);
        Assert.Equal(80, stats[0].Average);
        Assert.Equal(1, stats[1].SampleCount);
        Assert.Equal(0, stats[2].SampleCount);
        Assert.Null(stats[2].Average);
        Assert.Equal(40, stats[3].Average);
    }

    [Fact]
    public void AnalyzeDurationVsRecovery_BucketsHours()
    {
        var stats = SleepAnalyzer.AnalyzeDurationVsRecovery(
        [
            Night(10, recovery: 50, sleepHours: 5.5),
            Night(11, recovery: 70, sleepHours: 7.5),
            Night(12, recovery: 90, sleepHours: 7.9),
            Night(13, recovery: 80, sleepHours: 8.4),
        ]);

        Assert.Equal(1, stats[0].SampleCount);          // < 6 h
        Assert.Equal(2, stats[2].SampleCount);          // 7–8 h
        Assert.Equal(80, stats[2].Average);             // (70 + 90) / 2
        Assert.Equal(1, stats[3].SampleCount);          // > 8 h
    }

    [Fact]
    public void AnalyzeEveningTraining_ComparesNightsAfterLateWorkouts()
    {
        // Workout endet 19:30 Berlin am 10.06. (17:30 UTC) → Nacht zum 11.06. ist „nach Abendtraining".
        var workouts = new[]
        {
            new WhoopWorkout("w1", "weightlifting", Utc(10, 16, 30), Utc(10, 17, 30), null, 0),
            new WhoopWorkout("w2", "running", Utc(12, 5, 0), Utc(12, 5, 30), 5000, 0), // morgens → egal
        };

        var impact = SleepAnalyzer.AnalyzeEveningTraining(
        [
            Night(11, performance: 80),
            Night(12, performance: 90),
            Night(13, performance: 94),
        ], workouts);

        Assert.NotNull(impact);
        Assert.Equal(1, impact!.EveningNights);
        Assert.Equal(80, impact.AvgSleepPerformanceAfterEvening);
        Assert.Equal(2, impact.OtherNights);
        Assert.Equal(92, impact.AvgSleepPerformanceOther);
    }

    [Fact]
    public void AnalyzeEveningTraining_NullWhenAGroupIsEmpty()
    {
        Assert.Null(SleepAnalyzer.AnalyzeEveningTraining([Night(11, performance: 80)], []));
    }

    [Fact]
    public void BestBucket_RespectsMinSample()
    {
        var stats = new List<SleepBucketStats>
        {
            new("A", 6, 70),
            new("B", 6, 85),
            new("C", 2, 99), // zu wenig Daten
        };

        Assert.Equal("B", SleepAnalyzer.BestBucket(stats)!.Label);
        Assert.Null(SleepAnalyzer.BestBucket([new SleepBucketStats("A", 3, 70)]));
    }
}
