namespace Dashboard.Tests.Running;

public class RunReviewCalculatorTests
{
    private static Run Run(int year, int month, double km, int minutes = 30, double? elevation = 50) =>
        new(year * 10000 + month * 100 + (int)km, "Lauf", "Run",
            new DateTimeOffset(year, month, 15, 6, 0, 0, TimeSpan.Zero),
            km * 1000, TimeSpan.FromMinutes(minutes), [],
            ElevationGainMeters: elevation);

    [Fact]
    public void AvailableYears_DistinctDescending()
    {
        var years = RunReviewCalculator.AvailableYears(
            [Run(2025, 5, 5), Run(2026, 1, 8), Run(2025, 11, 6)]);

        Assert.Equal([2026, 2025], years);
    }

    [Fact]
    public void Build_AggregatesTotalsAndMonths_ForSelectedYear()
    {
        var review = RunReviewCalculator.Build(
        [
            Run(2026, 1, 10, minutes: 60, elevation: 100),
            Run(2026, 1, 5, minutes: 30, elevation: 40),
            Run(2026, 3, 8, minutes: 48, elevation: 60),
            Run(2025, 6, 20),   // anderes Jahr → ignoriert
        ], 2026);

        Assert.Equal(2026, review.Year);
        Assert.Equal(3, review.RunCount);
        Assert.Equal(23, review.TotalKm, 3);                 // 10 + 5 + 8
        Assert.Equal(200, review.TotalElevationMeters, 3);   // 100 + 40 + 60
        Assert.Equal(TimeSpan.FromMinutes(138), review.TotalTime);
        Assert.Equal(12, review.Months.Count);
        Assert.Equal(15, review.Months[0].Km, 3);            // Januar: 10 + 5
        Assert.Equal(8, review.Months[2].Km, 3);             // März
        Assert.Equal(0, review.Months[1].Km, 3);             // Februar leer
    }

    [Fact]
    public void Build_ComputesRecords()
    {
        var review = RunReviewCalculator.Build(
        [
            Run(2026, 1, 10, minutes: 60), // 6:00 /km
            Run(2026, 2, 6, minutes: 30),  // 5:00 /km → schnellste Pace
            Run(2026, 2, 7, minutes: 42),
        ], 2026);

        Assert.Equal(10, review.Records.LongestKm, 3);
        Assert.Equal(5.0, review.Records.FastestPaceMinPerKm!.Value, 3);
        Assert.Equal(2, review.Records.BiggestMonth);        // Februar: 6 + 7 = 13 km
        Assert.Equal(13, review.Records.BiggestMonthKm, 3);
    }

    [Fact]
    public void Build_EmptyYear_HasNoRecords()
    {
        var review = RunReviewCalculator.Build([Run(2025, 1, 5)], 2026);

        Assert.Equal(0, review.RunCount);
        Assert.Equal(0, review.Records.LongestKm);
        Assert.Null(review.Records.FastestPaceMinPerKm);
        Assert.Null(review.Records.BiggestMonth);
        Assert.Equal(0, review.EddingtonKm);
    }

    [Theory]
    [InlineData(new double[] { }, 0)]
    [InlineData(new[] { 3.0, 2.0, 1.0 }, 2)]            // 2 Läufe ≥ 2 km, aber nur einer ≥ 3
    [InlineData(new[] { 10.0, 8.0, 6.0, 5.0, 4.0 }, 4)] // 4. Lauf (5 km) ≥ 4, der 5. (4 km) bricht
    [InlineData(new[] { 5.0, 5.0, 5.0, 5.0, 5.0 }, 5)]
    public void Eddington_CountsRunsAtLeastEKm(double[] distances, int expected)
    {
        Assert.Equal(expected, RunReviewCalculator.Eddington(distances));
    }
}
