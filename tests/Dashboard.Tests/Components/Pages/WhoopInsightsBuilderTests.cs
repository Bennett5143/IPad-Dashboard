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

    private static WhoopWorkout TodWorkout(
        int day, int hourUtc, string sport = "running", int? avgHr = 150, double? kilojoule = null) =>
        new("tod-" + day + "-" + hourUtc, sport,
            new DateTimeOffset(2026, 6, day, hourUtc, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, day, hourUtc, 30, 0, TimeSpan.Zero),
            5000, 0,
            Kilojoule: kilojoule,
            AverageHeartRate: avgHr);

    [Fact]
    public void BuildTimeOfDayCards_OmitsEmptyCategories_AndShowsSamples()
    {
        // 6 Morgenläufe (05:00 UTC = früh) → genug für eine Aussage; kein Kraft/Seil.
        var workouts = Enumerable.Range(1, 6).Select(d => TodWorkout(d, 5)).ToList();

        var cards = WhoopInsightsBuilder.BuildTimeOfDayCards(workouts);

        var card = Assert.Single(cards);               // nur Laufen
        Assert.Equal("Laufen", card.Title);
        Assert.Equal(6, card.Rows.Count);              // alle Zeitfenster, auch leere
        var early = card.Rows.Single(r => r.BucketLabel == "früh");
        Assert.Equal(6, early.Count);
        Assert.True(early.IsBest);
        Assert.Equal("900", early.ValueLabel);         // 150 bpm × 30 min ÷ 5 km
        Assert.StartsWith("Stärkstes Zeitfenster: früh", card.Verdict, StringComparison.Ordinal);
        Assert.Contains("n = 6", card.Verdict, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildTimeOfDayCards_WithoutEnoughSamples_ExplainsInsteadOfGuessing()
    {
        var cards = WhoopInsightsBuilder.BuildTimeOfDayCards([TodWorkout(1, 5), TodWorkout(2, 17)]);

        var card = Assert.Single(cards);
        Assert.DoesNotContain("Stärkstes Zeitfenster", card.Verdict, StringComparison.Ordinal);
        Assert.Contains("mind. 5", card.Verdict, StringComparison.Ordinal);
        Assert.All(card.Rows.Where(r => r.Count > 0), r => Assert.True(r.LowSample));
    }

    [Fact]
    public void BuildSleepInsights_FormatsConsistencyBucketsAndEvening()
    {
        // 6 Nächte 23:00 Berlin (21:00 UTC, CEST), Recovery 80, Performance 90, 7,5 h.
        var metrics = Enumerable.Range(10, 6)
            .Select(d => Metric(d, recovery: 80, sleepHours: 7.5) with
            {
                SleepStartUtc = new DateTimeOffset(2026, 6, d - 1, 21, 0, 0, TimeSpan.Zero),
                SleepPerformance = 90
            })
            .ToList();
        // Abendtraining am 09.06. (Ende 19:30 Berlin) → Nacht zum 10.06.
        var workouts = new[]
        {
            new WhoopWorkout("e1", "weightlifting",
                new DateTimeOffset(2026, 6, 9, 16, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 9, 17, 30, 0, TimeSpan.Zero), null, 0)
        };

        var view = WhoopInsightsBuilder.BuildSleepInsights(metrics, workouts);

        Assert.NotNull(view);
        Assert.Equal("Ø Einschlafzeit 23:00 ± 0 min (n = 6)", view!.ConsistencyLabel);
        var bucket = view.BedtimeRows.Single(r => r.Count > 0);
        Assert.Equal("22:30–23:30", bucket.Label);
        Assert.Equal("80 %", bucket.ValueLabel);
        Assert.True(bucket.IsBest);
        Assert.StartsWith("Beste Ø-Recovery: 22:30–23:30", view.BedtimeVerdict, StringComparison.Ordinal);
        Assert.StartsWith("Beste Ø-Recovery: 7–8 h", view.DurationVerdict, StringComparison.Ordinal);
        Assert.Contains("(n = 1)", view.EveningLabel, StringComparison.Ordinal);   // 1 Abend-Nacht
        Assert.Contains("(n = 5)", view.EveningLabel, StringComparison.Ordinal);   // 5 übrige
    }

    [Fact]
    public void BuildSleepInsights_NullWithoutSleepData()
    {
        Assert.Null(WhoopInsightsBuilder.BuildSleepInsights([Metric(10, recovery: 70)], []));
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
