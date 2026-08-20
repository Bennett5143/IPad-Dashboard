using Dashboard.Web.Components.Pages;

namespace Dashboard.Tests.Components.Pages;

public class RunViewBuilderTests
{
    [Fact]
    public void BuildRunPlaces_FormatsSummaries()
    {
        var lastRun = new DateTimeOffset(2026, 8, 12, 17, 0, 0, TimeSpan.Zero);

        var row = Assert.Single(RunViewBuilder.BuildRunPlaces(
        [
            new RunPlaceSummary(1, "Alster", 7, 52.4, 5.5, lastRun),
        ]));

        Assert.Equal("Alster", row.Name);
        Assert.Equal("7×", row.Runs);
        Assert.Equal("52,4 km", row.Distance);  // Gesamtdistanz, nicht Ø je Lauf
        Assert.Equal("5:30 /km", row.Pace);     // 5,5 min/km
        Assert.Equal("12.08.2026", row.LastRun);
    }

    [Fact]
    public void BuildRunPlaces_HandlesMissingPaceAndDate()
    {
        var row = Assert.Single(RunViewBuilder.BuildRunPlaces(
            [new RunPlaceSummary(2, "Ort 2", 1, 4.0, null, null)]));

        Assert.Equal("–", row.Pace);
        Assert.Equal("–", row.LastRun);
    }
}
