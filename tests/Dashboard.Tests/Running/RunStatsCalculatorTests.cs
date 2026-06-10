namespace Dashboard.Tests.Running;

public class RunStatsCalculatorTests
{
    private static Run Make(double meters, int minutes) =>
        new(1, "Lauf", "Run", new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero),
            meters, TimeSpan.FromMinutes(minutes), []);

    [Fact]
    public void Calculate_AggregatesDistanceCountAndPace()
    {
        var runs = new[] { Make(5000, 25), Make(10000, 50) };

        var stats = RunStatsCalculator.Calculate(runs);

        Assert.Equal(2, stats.RunCount);
        Assert.Equal(15, stats.TotalDistanceKm, 3);
        Assert.Equal(TimeSpan.FromMinutes(75), stats.TotalMovingTime);
        Assert.Equal(5.0, stats.AveragePaceMinPerKm!.Value, 3); // 75 min / 15 km
    }

    [Fact]
    public void Calculate_EmptySet_HasNoPace()
    {
        var stats = RunStatsCalculator.Calculate([]);

        Assert.Equal(0, stats.RunCount);
        Assert.Equal(0, stats.TotalDistanceKm);
        Assert.Null(stats.AveragePaceMinPerKm);
    }
}
