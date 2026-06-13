using Dashboard.Web.Components.Pages;

namespace Dashboard.Tests.Components.Pages;

public class RunReviewViewBuilderTests
{
    private static Run Run(int year, int month, double km, int minutes = 30, double? elevation = 50) =>
        new(year * 10000 + month * 100 + (int)km, "Lauf", "Run",
            new DateTimeOffset(year, month, 15, 6, 0, 0, TimeSpan.Zero),
            km * 1000, TimeSpan.FromMinutes(minutes), [],
            ElevationGainMeters: elevation);

    [Fact]
    public void Build_NullWithoutRuns()
    {
        Assert.Null(RunReviewViewBuilder.Build([], 2026));
    }

    [Fact]
    public void Build_FormatsTotalsRecordsAndMonths()
    {
        var view = RunReviewViewBuilder.Build(
        [
            Run(2026, 1, 10, minutes: 60, elevation: 120),
            Run(2026, 1, 5, minutes: 25),                 // 5:00 /km → schnellste
            Run(2026, 3, 8, minutes: 48),
            Run(2025, 6, 12),                             // anderes Jahr
        ], 2026);

        Assert.NotNull(view);
        Assert.Equal(2026, view!.Year);
        Assert.Equal([2026, 2025], view.AvailableYears);
        Assert.Equal("3", view.RunCount);
        Assert.Equal("23,0 km", view.TotalKm);
        Assert.Equal("220 m", view.TotalElevation);       // 120 + 50 + 50 (Default-Elevation)
        Assert.Equal("E = 3", view.Eddington);            // 10,8,5 → E=3
        Assert.Equal("10,0 km", view.LongestRun);
        Assert.Equal("5:00 /km", view.FastestPace);
        Assert.Equal("Jan (15 km)", view.BiggestMonth);   // 10 + 5
        Assert.Equal(12, view.Months.Count);
        Assert.Equal("Jan", view.Months[0].Label);
        Assert.Equal(100, view.Months[0].BarPercent, 1);  // stärkster Monat → 100 %
        Assert.Equal(0, view.Months[1].BarPercent, 1);    // Februar leer
    }

    [Fact]
    public void Build_SwitchesYear()
    {
        var runs = new[] { Run(2026, 1, 10), Run(2025, 6, 7) };

        Assert.Equal("1", RunReviewViewBuilder.Build(runs, 2025)!.RunCount);
        Assert.Equal("7,0 km", RunReviewViewBuilder.Build(runs, 2025)!.TotalKm);
    }
}
