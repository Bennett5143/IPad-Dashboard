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

        Assert.Equal(6, cards.Count);
        var recovery = cards[0];
        Assert.Equal("Recovery", recovery.Title);
        Assert.Equal("40", recovery.Current);   // letzter Wert
        Assert.Equal("55", recovery.Avg);
        Assert.Equal("40", recovery.Min);
        Assert.Equal("70", recovery.Max);
    }

    [Fact]
    public void BuildCards_IncludesRespiratoryRateCard()
    {
        var cards = WhoopInsightsBuilder.BuildCards([Metric(10) with { RespiratoryRate = 14.2 }]);

        var resp = cards.Single(c => c.Title == "Atemfrequenz");
        Assert.Equal("14,2", resp.Current);
        Assert.Equal("/min", resp.Unit);
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

    [Fact]
    public void BuildRuns_AppendsAverageHeartRate_WhenPresent()
    {
        var runs = WhoopInsightsBuilder.BuildRuns(
            [Workout(10) with { AverageHeartRate = 152 }], []);

        Assert.Equal("30 min · 6,00 min/km · Ø 152 bpm", runs[0].Detail);
    }

    [Fact]
    public void BuildSleepNight_ReturnsNull_WithoutStageData()
    {
        Assert.Null(WhoopInsightsBuilder.BuildSleepNight([Metric(10, sleepHours: 7.5)]));
    }

    [Fact]
    public void BuildSleepNight_BuildsSegmentsSharesAndLabels()
    {
        var night = WhoopInsightsBuilder.BuildSleepNight(
        [
            Metric(10),
            Metric(11) with
            {
                LightSleepHours = 4.0,
                DeepSleepHours = 1.5,
                RemSleepHours = 2.0,
                AwakeHours = 0.5,
                SleepStartUtc = new DateTimeOffset(2026, 6, 10, 22, 0, 0, TimeSpan.Zero),
                SleepEndUtc = new DateTimeOffset(2026, 6, 11, 6, 0, 0, TimeSpan.Zero),
                RespiratoryRate = 14.2
            }
        ]);

        Assert.NotNull(night);
        Assert.Equal("11.06.", night!.DateLabel);
        Assert.Equal("00:00–08:00", night.TimeRange); // UTC 22–06 Uhr = Berlin (CEST) 0–8 Uhr
        Assert.Equal("7,5 h Schlaf", night.AsleepLabel);
        Assert.Equal("Ø 14,2 Atemzüge/min", night.RespiratoryLabel);

        Assert.Equal(4, night.Segments.Count);
        var light = night.Segments[0];
        Assert.Equal("Leicht", light.Label);
        Assert.Equal(50, light.WidthPercent);          // 4 von 8 h gesamt
        Assert.Equal("4,0 h · 50 %", light.Detail);
        var awake = night.Segments[^1];
        Assert.Equal("Wach", awake.Label);
        Assert.Equal(6.25, awake.WidthPercent);
    }

    [Fact]
    public void BuildSleepNight_PicksLatestNight_AndOmitsOptionalParts()
    {
        var night = WhoopInsightsBuilder.BuildSleepNight(
        [
            Metric(10) with { LightSleepHours = 4.0, DeepSleepHours = 2.0, RemSleepHours = 1.0 },
            Metric(9) with { LightSleepHours = 3.0, DeepSleepHours = 1.0, RemSleepHours = 1.0 }
        ]);

        Assert.Equal("10.06.", night!.DateLabel);
        Assert.Null(night.TimeRange);                  // keine Schlafzeiten geliefert
        Assert.Null(night.RespiratoryLabel);
        Assert.Equal(3, night.Segments.Count);         // ohne Wachzeit kein Wach-Segment
    }
}
