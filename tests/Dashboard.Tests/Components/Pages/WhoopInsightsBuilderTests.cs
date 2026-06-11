using Dashboard.Domain.Whoop;
using Dashboard.Web.Components.Pages;

namespace Dashboard.Tests.Components.Pages;

public class WhoopInsightsBuilderTests
{
    private static WhoopDailyMetric Metric(
        int day, int? recovery = null, double? sleepHours = null) =>
        new(new DateOnly(2026, 6, day), recovery, 60, 50, sleepHours, 90, 11.0);

    private static WhoopWorkout Workout(
        int day, string sport = "running", double highShare = 0, double? distanceM = 5000) =>
        new("w" + day, sport,
            new DateTimeOffset(2026, 6, day, 6, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, day, 6, 30, 0, TimeSpan.Zero),
            distanceM, highShare);

    [Fact]
    public void BuildCards_ComputesCurrentAvgMinMax_GermanFormatted()
    {
        var cards = WhoopInsightsBuilder.BuildCards([Metric(10, recovery: 70), Metric(11, recovery: 40)]);

        Assert.Equal(5, cards.Count);
        var recovery = cards[0];
        Assert.Equal("Recovery", recovery.Title);
        Assert.Equal("40", recovery.Current);   // letzter Wert
        Assert.Equal("55", recovery.Avg);
        Assert.Equal("40", recovery.Min);
        Assert.Equal("70", recovery.Max);
    }

    [Fact]
    public void BuildCards_UsesDecimalComma_ForSleepHours()
    {
        var cards = WhoopInsightsBuilder.BuildCards([Metric(10, sleepHours: 7.5)]);

        Assert.Equal("7,5", cards.Single(c => c.Title == "Schlaf").Current);
    }

    [Fact]
    public void BuildCards_ShowsDashes_WithoutData()
    {
        var card = WhoopInsightsBuilder.BuildCards([]).First();

        Assert.Equal("–", card.Current);
        Assert.Equal("–", card.Avg);
        Assert.Empty(card.Values);
    }

    [Fact]
    public void BuildRuns_KeepsOnlyRuns_NewestFirst_WithRecoveryColour()
    {
        var history = new[] { Metric(10, recovery: 70) };
        var workouts = new[]
        {
            Workout(10),                              // Zone-2-Lauf, Tag mit grüner Recovery
            Workout(11, sport: "cycling"),            // kein Lauf → raus
            Workout(11, highShare: 0.3, distanceM: null) // VO2max-Lauf ohne Distanz, Tag ohne Recovery
        };

        var runs = WhoopInsightsBuilder.BuildRuns(workouts, history);

        Assert.Equal(2, runs.Count);
        Assert.Equal("VO2max", runs[0].Kind);            // neuester zuerst (11.)
        Assert.Equal("30 min", runs[0].Detail);          // ohne Distanz keine Pace
        Assert.Equal("recovery-none", runs[0].RecoveryCss);
        Assert.Equal("Zone 2", runs[1].Kind);
        Assert.Equal("30 min · 6,00 min/km", runs[1].Detail);
        Assert.Equal("recovery-high", runs[1].RecoveryCss);
        Assert.Equal("10.06.", runs[1].Date);
    }
}
