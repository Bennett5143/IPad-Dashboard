using Dashboard.Web.Components.Pages;

namespace Dashboard.Tests.Components.Pages;

public class RunViewBuilderTests
{
    private static Run Run(
        long id = 1, string name = "Morgenlauf", double meters = 5000, int minutes = 30,
        double? elevation = 42, int? avgHr = 150) =>
        new(id, name, "Run",
            new DateTimeOffset(2026, 6, 1, 4, 30, 0, TimeSpan.Zero), // 06:30 Berlin (CEST)
            meters, TimeSpan.FromMinutes(minutes), [],
            ElevationGainMeters: elevation, AverageHeartRate: avgHr);

    [Fact]
    public void BuildList_FormatsRowFields()
    {
        var row = Assert.Single(RunViewBuilder.BuildList([Run()]));

        Assert.Equal(1, row.Id);
        Assert.Equal("01.06.2026", row.Date);
        Assert.Equal("Morgenlauf", row.Name);
        Assert.Equal("5,0 km", row.Distance);
        Assert.Equal("6:00 /km", row.Pace);
        Assert.Equal("Ø 150 bpm", row.HeartRate);
        Assert.Equal("42 m", row.Elevation);
    }

    [Fact]
    public void BuildList_UsesDashesAndFallbackName()
    {
        var row = Assert.Single(RunViewBuilder.BuildList([Run(name: " ", elevation: null, avgHr: null)]));

        Assert.Equal("Lauf", row.Name);
        Assert.Equal("–", row.HeartRate);
        Assert.Equal("–", row.Elevation);
    }

    [Fact]
    public void BuildDetailHeader_FormatsDurationAndDate()
    {
        var header = RunViewBuilder.BuildDetailHeader(Run(minutes: 75));

        Assert.Contains("01.06.2026", header.Date, StringComparison.Ordinal);
        Assert.Contains("06:30", header.Date, StringComparison.Ordinal);   // Berlin-Zeit
        Assert.Equal("1:15:00 h", header.Duration);                        // > 1 h
        Assert.Equal("15:00 /km", header.Pace);                            // 75 min / 5 km
    }

    [Fact]
    public void BuildDetailHeader_ShortDuration_InMinutes()
    {
        Assert.Equal("30:00 min", RunViewBuilder.BuildDetailHeader(Run(minutes: 30)).Duration);
    }
}
