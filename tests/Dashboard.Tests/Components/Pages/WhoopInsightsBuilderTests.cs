using Dashboard.Domain.Whoop;
using Dashboard.Web.Components.Pages;

namespace Dashboard.Tests.Components.Pages;

public class WhoopInsightsBuilderTests
{
    private static WhoopDailyMetric Metric(
        int day, int? recovery = null, double? sleepHours = null) =>
        new(new DateOnly(2026, 6, day), recovery, 60, 50, sleepHours, 90, 11.0);

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

    private static WhoopWorkout TodWorkout(
        int day, int hourUtc, string sport = "running", int? avgHr = 150, double? kilojoule = null) =>
        new("tod-" + day + "-" + hourUtc, sport,
            new DateTimeOffset(2026, 6, day, hourUtc, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, day, hourUtc, 30, 0, TimeSpan.Zero),
            5000, 0,
            Kilojoule: kilojoule,
            AverageHeartRate: avgHr);

    [Fact]
    public void BuildSleepInsights_FormatsConsistencyAndBuckets()
    {
        // 6 Nächte 23:00 Berlin (21:00 UTC, CEST), Recovery 80, Performance 90, 7,5 h.
        var metrics = Enumerable.Range(10, 6)
            .Select(d => Metric(d, recovery: 80, sleepHours: 7.5) with
            {
                SleepStartUtc = new DateTimeOffset(2026, 6, d - 1, 21, 0, 0, TimeSpan.Zero),
                SleepPerformance = 90
            })
            .ToList();

        var view = WhoopInsightsBuilder.BuildSleepInsights(metrics);

        Assert.NotNull(view);
        Assert.Equal("Ø Einschlafzeit 23:00 ± 0 min (n = 6)", view!.ConsistencyLabel);
        var bucket = view.BedtimeRows.Single(r => r.Count > 0);
        Assert.Equal("22:30–23:30", bucket.Label);
        Assert.Equal("80 %", bucket.ValueLabel);
        Assert.True(bucket.IsBest);
        Assert.StartsWith("Beste Ø-Recovery: 22:30–23:30", view.BedtimeVerdict, StringComparison.Ordinal);
        Assert.StartsWith("Beste Ø-Recovery: 7–8 h", view.DurationVerdict, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSleepInsights_NullWithoutSleepData()
    {
        Assert.Null(WhoopInsightsBuilder.BuildSleepInsights([Metric(10, recovery: 70)]));
    }

    [Fact]
    public void BuildTrainingLoad_FormatsRatioZoneAndSparkline()
    {
        // 90 Tage konstanter Strain 10 → ACWR ≈ 1, Zone „ausgewogen".
        var metrics = Enumerable.Range(0, 90)
            .Select(i => new WhoopDailyMetric(
                new DateOnly(2026, 1, 1).AddDays(i), null, null, null, null, null, 10.0))
            .ToList();

        var view = WhoopInsightsBuilder.BuildTrainingLoad(metrics);

        Assert.NotNull(view);
        Assert.Equal("ausgewogen", view!.ZoneLabel);
        Assert.Equal("load-ok", view.ZoneCss);
        Assert.Null(view.ConfidenceHint);                 // alle 7 Akut-Tage mit Daten
        Assert.Equal(90, view.Sparkline.Count);
        Assert.Contains("Form-Heuristik", view.MethodHint, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildTrainingLoad_NullDuringWarmup_HintOnThinAcuteWindow()
    {
        // Nur 10 Tage Historie → chronische EWMA im Warmlauf.
        var warmup = Enumerable.Range(0, 10)
            .Select(i => new WhoopDailyMetric(
                new DateOnly(2026, 1, 1).AddDays(i), null, null, null, null, null, 10.0))
            .ToList();
        Assert.Null(WhoopInsightsBuilder.BuildTrainingLoad(warmup));

        // Dünn besetztes Akut-Fenster: nach 50 lückenlosen Tagen nur noch Tag 52 und 56
        // mit Strain → im 7-Tage-Fenster bis zum letzten Datenpunkt liegen 2 Tage.
        var thin = Enumerable.Range(0, 57)
            .Select(i => new WhoopDailyMetric(
                new DateOnly(2026, 1, 1).AddDays(i), null, null, null, null, null,
                i < 50 || i is 52 or 56 ? 10.0 : null))
            .ToList();
        var view = WhoopInsightsBuilder.BuildTrainingLoad(thin);

        Assert.NotNull(view);
        Assert.Contains("Nur 2 von 7 Tagen", view!.ConfidenceHint, StringComparison.Ordinal);
    }

    private static Run EffRun(int month, int day, int minutes = 30, int? avgHr = 150) =>
        new(month * 100 + day, "Lauf", "Run",
            new DateTimeOffset(2026, month, day, 6, 0, 0, TimeSpan.Zero),
            5000, TimeSpan.FromMinutes(minutes), [],
            AverageHeartRate: avgHr);

    [Fact]
    public void BuildFitnessCurve_FormatsCurrentAndTrend()
    {
        var view = WhoopInsightsBuilder.BuildFitnessCurve(
        [
            EffRun(2, 3), EffRun(2, 10),                       // Feb: Ø 900
            EffRun(5, 7, minutes: 27), EffRun(5, 14, minutes: 27) // Mai: Ø 810 → −10 %
        ]);

        Assert.NotNull(view);
        Assert.Equal("Ø 810 Schläge/km (n = 2)", view!.CurrentLabel);
        Assert.Equal("10,0 % effizienter als vor ~3 Monaten", view.TrendLabel);
        Assert.Equal("trend-good", view.TrendCss);
        Assert.Equal(4, view.Sparkline.Count);                 // Feb–Mai
        Assert.Contains("Heuristik", view.Hint, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFitnessCurve_NullWithoutQualifyingMonth()
    {
        Assert.Null(WhoopInsightsBuilder.BuildFitnessCurve([]));
        Assert.Null(WhoopInsightsBuilder.BuildFitnessCurve([EffRun(5, 7)])); // 1 Lauf < Min-Stichprobe
    }

    [Fact]
    public void BuildRecoveryDrivers_FormatsRowsAndScatters()
    {
        // 14 Tage: Recovery steigt mit Schlafdauer (perfekt positiv), Strain variiert.
        var metrics = Enumerable.Range(1, 14)
            .Select(d => Metric(d, recovery: 50 + d, sleepHours: 6 + d * 0.1) with
            {
                DayStrain = 8.0 + (d % 3)
            })
            .ToList();

        var view = WhoopInsightsBuilder.BuildRecoveryDrivers(metrics);

        Assert.NotNull(view);
        var sleep = view!.Rows.Single(r => r.Label == "Schlafdauer");
        Assert.Equal("+1,00", sleep.RLabel);
        Assert.Equal("stark", sleep.StrengthLabel);
        Assert.Equal(100, sleep.BarPercent, 1);
        var bedtime = view.Rows.Single(r => r.Label == "Einschlafzeit (später)");
        Assert.Equal("–", bedtime.RLabel);                     // keine Schlafzeiten gesetzt
        Assert.Equal("zu wenig Daten", bedtime.StrengthLabel);
        Assert.Equal(2, view.Scatters.Count);                  // Schlafdauer + Vortages-Strain
        Assert.Contains("keine Kausalität", view.Hint, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildRecoveryDrivers_NullWithoutAnyPairs()
    {
        Assert.Null(WhoopInsightsBuilder.BuildRecoveryDrivers([]));
    }

    [Fact]
    public void BuildTimeOfDayMatrix_MapsCountsToIntensities()
    {
        // 01.06.2026 = Montag, 05:00 UTC = früh.
        var matrix = WhoopInsightsBuilder.BuildTimeOfDayMatrix(
            [TodWorkout(1, 5), TodWorkout(1, 17, sport: "weightlifting", kilojoule: 600)]);

        Assert.Equal(["Mo", "Di", "Mi", "Do", "Fr", "Sa", "So"], matrix.DayLabels);
        var early = matrix.Rows.Single(r => r.BucketLabel == "früh");
        Assert.Equal(1, early.Cells[0].Count);          // Montag
        Assert.Equal("cell-1", early.Cells[0].Css);
        Assert.Equal("cell-0", early.Cells[1].Css);     // Dienstag leer
        var evening = matrix.Rows.Single(r => r.BucketLabel == "abends");
        Assert.Equal(1, evening.Cells[0].Count);        // 17:00 UTC = 19:00 Berlin
    }
}
