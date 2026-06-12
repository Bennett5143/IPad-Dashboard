using Dashboard.Domain.Whoop;

namespace Dashboard.Tests.Whoop;

public class TrainingLoadCalculatorTests
{
    private static WhoopDailyMetric Day(DateOnly date, double? strain) =>
        new(date, null, null, null, null, null, strain);

    private static List<WhoopDailyMetric> ConstantLoad(DateOnly from, int days, double strain) =>
        Enumerable.Range(0, days).Select(i => Day(from.AddDays(i), strain)).ToList();

    private static readonly DateOnly Start = new(2026, 1, 1);

    [Fact]
    public void Compute_ConvergesTowardsOne_UnderConstantLoad()
    {
        var points = TrainingLoadCalculator.Compute(ConstantLoad(Start, 90, 10));

        var latest = points[^1];
        Assert.NotNull(latest.Ratio);
        Assert.InRange(latest.Ratio!.Value, 0.95, 1.15); // akut ≈ chronisch
        Assert.Equal(TrainingLoadZone.Balanced, TrainingLoadCalculator.ZoneFor(latest.Ratio.Value));
    }

    [Fact]
    public void Compute_SpikesAboveOnSuddenIncrease()
    {
        var metrics = ConstantLoad(Start, 90, 8);
        metrics.AddRange(ConstantLoad(Start.AddDays(90), 7, 20)); // harte Woche

        var latest = TrainingLoadCalculator.Compute(metrics)[^1];

        Assert.True(latest.Ratio > 1.3, $"Ratio {latest.Ratio} sollte deutlich erhöht sein.");
    }

    [Fact]
    public void Compute_HasNoRatio_DuringWarmup()
    {
        var points = TrainingLoadCalculator.Compute(ConstantLoad(Start, 40, 10));

        Assert.Null(points[10].Ratio);                                   // Warmlauf
        Assert.Null(points[TrainingLoadCalculator.ChronicDays - 1].Ratio);
        Assert.NotNull(points[TrainingLoadCalculator.ChronicDays].Ratio); // ab Tag 28
    }

    [Fact]
    public void Compute_FillsCalendarGaps_AsRestDays()
    {
        var points = TrainingLoadCalculator.Compute(
            [Day(Start, 10), Day(Start.AddDays(4), 10)]); // 3 Tage Lücke

        Assert.Equal(5, points.Count);                    // lückenlose Kalenderreihe
        Assert.True(points[1].Acute < points[0].Acute);   // Lücken-Tag = Last 0 → EWMA fällt
    }

    [Fact]
    public void ZoneFor_MapsBoundaries()
    {
        Assert.Equal(TrainingLoadZone.Low, TrainingLoadCalculator.ZoneFor(0.79));
        Assert.Equal(TrainingLoadZone.Balanced, TrainingLoadCalculator.ZoneFor(0.8));
        Assert.Equal(TrainingLoadZone.Balanced, TrainingLoadCalculator.ZoneFor(1.29));
        Assert.Equal(TrainingLoadZone.Elevated, TrainingLoadCalculator.ZoneFor(1.3));
        Assert.Equal(TrainingLoadZone.High, TrainingLoadCalculator.ZoneFor(1.5));
    }

    [Fact]
    public void AcuteDaysWithData_CountsStrainDaysInWindow()
    {
        var last = Start.AddDays(40);
        var metrics = new List<WhoopDailyMetric>
        {
            Day(last, 10),
            Day(last.AddDays(-2), 12),
            Day(last.AddDays(-6), 9),
            Day(last.AddDays(-7), 9),  // außerhalb des 7-Tage-Fensters
            Day(last.AddDays(-1), null) // ohne Strain → zählt nicht
        };

        Assert.Equal(3, TrainingLoadCalculator.AcuteDaysWithData(metrics, last));
    }
}
