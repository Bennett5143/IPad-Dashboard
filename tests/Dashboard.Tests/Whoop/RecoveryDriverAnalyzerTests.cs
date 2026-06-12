using Dashboard.Domain.Whoop;

namespace Dashboard.Tests.Whoop;

public class RecoveryDriverAnalyzerTests
{
    private static WhoopDailyMetric Day(
        int day, int? recovery = null, double? sleepHours = null, double? strain = null,
        DateTimeOffset? sleepStartUtc = null) =>
        new(new DateOnly(2026, 5, day), recovery, null, null, sleepHours, null, strain,
            SleepStartUtc: sleepStartUtc);

    [Fact]
    public void Pearson_DetectsPerfectCorrelation_AndNeedsMinSamples()
    {
        var positive = Enumerable.Range(0, 12).Select(i => ((double)i, 2.0 * i + 1)).ToList();
        Assert.Equal(1.0, RecoveryDriverAnalyzer.Pearson(positive)!.Value, 6);

        var negative = Enumerable.Range(0, 12).Select(i => ((double)i, 100.0 - i)).ToList();
        Assert.Equal(-1.0, RecoveryDriverAnalyzer.Pearson(negative)!.Value, 6);

        Assert.Null(RecoveryDriverAnalyzer.Pearson(positive.Take(9).ToList())); // < MinSamples
        Assert.Null(RecoveryDriverAnalyzer.Pearson(
            Enumerable.Range(0, 12).Select(i => (5.0, (double)i)).ToList())); // keine X-Varianz
    }

    [Fact]
    public void Pairs_PreviousDayStrain_MatchesStrainToNextDayRecovery()
    {
        var pairs = RecoveryDriverAnalyzer.Pairs(
        [
            Day(1, strain: 15),
            Day(2, recovery: 40, strain: 8),   // Paar: (15, 40)
            Day(3, recovery: 80),              // Paar: (8, 80)
            Day(5, recovery: 70),              // Vortag (4.) fehlt → kein Paar
        ], RecoveryFactor.PreviousDayStrain);

        Assert.Equal(2, pairs.Count);
        Assert.Contains((15.0, 40.0), pairs);
        Assert.Contains((8.0, 80.0), pairs);
    }

    [Fact]
    public void Pairs_Bedtime_UsesAnchorMinutes()
    {
        // 21:00 UTC = 23:00 Berlin (CEST) → 300 min nach 18:00.
        var pairs = RecoveryDriverAnalyzer.Pairs(
            [Day(2, recovery: 70, sleepStartUtc: new DateTimeOffset(2026, 5, 1, 21, 0, 0, TimeSpan.Zero))],
            RecoveryFactor.Bedtime);

        Assert.Equal((300.0, 70.0), Assert.Single(pairs));
    }

    [Fact]
    public void Analyze_ReturnsAllFactors_WithSampleCounts()
    {
        var metrics = Enumerable.Range(1, 14)
            .Select(d => Day(d, recovery: 50 + d, sleepHours: 6 + d * 0.1, strain: 10))
            .ToList();

        var stats = RecoveryDriverAnalyzer.Analyze(metrics);

        Assert.Equal(3, stats.Count);
        var sleep = stats.Single(s => s.Factor == RecoveryFactor.SleepDuration);
        Assert.Equal(14, sleep.SampleCount);
        Assert.Equal(1.0, sleep.PearsonR!.Value, 4);   // konstruiert perfekt linear
        var bedtime = stats.Single(s => s.Factor == RecoveryFactor.Bedtime);
        Assert.Equal(0, bedtime.SampleCount);          // keine Schlafzeiten gesetzt
        Assert.Null(bedtime.PearsonR);
        var strain = stats.Single(s => s.Factor == RecoveryFactor.PreviousDayStrain);
        Assert.Equal(13, strain.SampleCount);          // Tag 1 hat keinen Vortag
        Assert.Null(strain.PearsonR);                  // Strain konstant → keine Varianz
    }
}
