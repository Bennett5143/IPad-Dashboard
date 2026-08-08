namespace Dashboard.Tests.Weather;

public class WeatherSnapshotFactoryTests
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    // 10. Juni 2026, 12:00 UTC = 14:00 Berlin (CEST, UTC+2)
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);

    private static readonly CurrentWeather Current =
        new(17.4, 16.8, 70, 4.0, WeatherCondition.Clear, "Klarer himmel");

    private static ForecastStep Step(
        DateTimeOffset utc, double temp, double pop, WeatherCondition condition, string description = "") =>
        new(utc, temp, pop, condition, description);

    private static ForecastStep Utc(int day, int hour, double temp, double pop, WeatherCondition condition) =>
        Step(new DateTimeOffset(2026, 6, day, hour, 0, 0, TimeSpan.Zero), temp, pop, condition);

    [Fact]
    public void Today_AggregatesMinMaxAndMaxPop_OverTodaysSteps()
    {
        var steps = new[]
        {
            Utc(10, 13, 18, 0.1, WeatherCondition.Clear),
            Utc(10, 16, 20, 0.2, WeatherCondition.Clouds),
            Utc(10, 19, 15, 0.5, WeatherCondition.Rain)
        };

        var snapshot = WeatherSnapshotFactory.Create(Current, steps, NowUtc, BerlinTz, 4);

        Assert.Equal(new DateOnly(2026, 6, 10), snapshot.Today.Date);
        Assert.Equal(15, snapshot.Today.MinTemperature);
        Assert.Equal(20, snapshot.Today.MaxTemperature);
        Assert.Equal(0.5, snapshot.Today.PrecipitationProbability);
    }

    [Fact]
    public void DailyCondition_PrefersFrequency()
    {
        var steps = new[]
        {
            Utc(10, 13, 18, 0.1, WeatherCondition.Clouds),
            Utc(10, 16, 20, 0.2, WeatherCondition.Clouds),
            Utc(10, 19, 15, 0.1, WeatherCondition.Clear)
        };

        var snapshot = WeatherSnapshotFactory.Create(Current, steps, NowUtc, BerlinTz, 4);

        Assert.Equal(WeatherCondition.Clouds, snapshot.Today.Condition);
    }

    [Fact]
    public void DailyCondition_OnTie_PrefersMoreSevere()
    {
        // Drei Zustände, jeder genau einmal → Gleichstand → der schwerste (Regen) gewinnt.
        var steps = new[]
        {
            Utc(10, 13, 18, 0.1, WeatherCondition.Clear),
            Utc(10, 16, 20, 0.2, WeatherCondition.Clouds),
            Utc(10, 19, 15, 0.5, WeatherCondition.Rain)
        };

        var snapshot = WeatherSnapshotFactory.Create(Current, steps, NowUtc, BerlinTz, 4);

        Assert.Equal(WeatherCondition.Rain, snapshot.Today.Condition);
    }

    [Fact]
    public void Outlook_AggregatesTomorrowSeparately()
    {
        var steps = new[]
        {
            Utc(10, 13, 18, 0.1, WeatherCondition.Clear),
            Utc(11, 7, 12, 0.6, WeatherCondition.Clouds),
            Utc(11, 10, 16, 0.3, WeatherCondition.Clouds)
        };

        var snapshot = WeatherSnapshotFactory.Create(Current, steps, NowUtc, BerlinTz, 4);

        Assert.Equal(2, snapshot.Outlook.Count);
        var tomorrow = snapshot.Outlook[1];
        Assert.Equal(new DateOnly(2026, 6, 11), tomorrow.Date);
        Assert.Equal(12, tomorrow.MinTemperature);
        Assert.Equal(16, tomorrow.MaxTemperature);
        Assert.Equal(0.6, tomorrow.PrecipitationProbability);
        Assert.Equal(WeatherCondition.Clouds, tomorrow.Condition);
    }

    [Fact]
    public void Outlook_StartsWithToday()
    {
        var steps = new[]
        {
            Utc(10, 13, 18, 0.1, WeatherCondition.Clear),
            Utc(11, 7, 12, 0.6, WeatherCondition.Clouds)
        };

        var snapshot = WeatherSnapshotFactory.Create(Current, steps, NowUtc, BerlinTz, 4);

        Assert.Equal(snapshot.Today, snapshot.Outlook[0]);
    }

    [Fact]
    public void Outlook_ContainsOnlyToday_WhenNoFutureSteps()
    {
        var steps = new[] { Utc(10, 13, 18, 0.1, WeatherCondition.Clear) };

        var snapshot = WeatherSnapshotFactory.Create(Current, steps, NowUtc, BerlinTz, 4);

        Assert.Single(snapshot.Outlook);
        Assert.Equal(snapshot.Today, snapshot.Outlook[0]);
    }

    [Fact]
    public void Outlook_CapsAtThreeDays()
    {
        // Vier Tage Vorhersage → nur heute + 2 Folgetage landen im Ausblick.
        var steps = new[]
        {
            Utc(10, 13, 18, 0.1, WeatherCondition.Clear),
            Utc(11, 13, 17, 0.2, WeatherCondition.Clouds),
            Utc(12, 13, 16, 0.3, WeatherCondition.Rain),
            Utc(13, 13, 15, 0.4, WeatherCondition.Clear)
        };

        var snapshot = WeatherSnapshotFactory.Create(Current, steps, NowUtc, BerlinTz, 4);

        Assert.Equal(3, snapshot.Outlook.Count);
        Assert.Equal(new DateOnly(2026, 6, 12), snapshot.Outlook[^1].Date);
    }

    [Fact]
    public void Today_IncludesCurrentTemperatureInMinMax()
    {
        // Nur noch ein Rest-Schritt heute mit 13°, aktuell aber 14°
        // → Spanne muss 13/14 sein, nicht 13/13 (sonst Widerspruch zur Ist-Temperatur).
        var current = new CurrentWeather(14, 14, 70, 4.0, WeatherCondition.Clear, "Klar");
        var steps = new[] { Utc(10, 16, 13, 0.1, WeatherCondition.Clear) };

        var snapshot = WeatherSnapshotFactory.Create(current, steps, NowUtc, BerlinTz, 4);

        Assert.Equal(13, snapshot.Today.MinTemperature);
        Assert.Equal(14, snapshot.Today.MaxTemperature);
    }

    [Fact]
    public void Today_FallsBackToCurrent_WhenNoStepsForToday()
    {
        // Nur Vorhersage-Schritte für morgen vorhanden.
        var steps = new[]
        {
            Utc(11, 7, 12, 0.6, WeatherCondition.Clouds),
            Utc(11, 10, 16, 0.3, WeatherCondition.Clouds)
        };

        var snapshot = WeatherSnapshotFactory.Create(Current, steps, NowUtc, BerlinTz, 4);

        Assert.Equal(new DateOnly(2026, 6, 10), snapshot.Today.Date);
        Assert.Equal(Current.Temperature, snapshot.Today.MinTemperature);
        Assert.Equal(Current.Temperature, snapshot.Today.MaxTemperature);
        Assert.Equal(WeatherCondition.Clear, snapshot.Today.Condition);
        Assert.Equal(0, snapshot.Today.PrecipitationProbability);
    }

    [Fact]
    public void Hourly_TakesFutureStepsInOrder_LocalizedAndCapped()
    {
        var steps = new[]
        {
            Utc(10, 9, 14, 0.0, WeatherCondition.Clear),   // 11:00 Berlin → vor "jetzt", fällt raus
            Utc(10, 13, 18, 0.1, WeatherCondition.Clear),  // 15:00
            Utc(10, 16, 20, 0.2, WeatherCondition.Clouds), // 18:00
            Utc(10, 19, 15, 0.5, WeatherCondition.Rain),   // 21:00
            Utc(11, 7, 12, 0.6, WeatherCondition.Clouds)   // 09:00 (nächster Tag) → 4. Eintrag
        };

        var snapshot = WeatherSnapshotFactory.Create(Current, steps, NowUtc, BerlinTz, 4);

        Assert.Equal(4, snapshot.Hourly.Count);
        Assert.Equal(15, snapshot.Hourly[0].Time.Hour);
        Assert.Equal(18, snapshot.Hourly[0].Temperature);
        Assert.Equal(0.1, snapshot.Hourly[0].PrecipitationProbability);
        Assert.Equal(9, snapshot.Hourly[3].Time.Hour);
        Assert.All(snapshot.Hourly, h => Assert.True(h.Time > TimeZoneInfo.ConvertTime(NowUtc, BerlinTz)));
    }

    [Fact]
    public void RetrievedAt_IsTheProvidedNow()
    {
        var snapshot = WeatherSnapshotFactory.Create(
            Current, Array.Empty<ForecastStep>(), NowUtc, BerlinTz, 4);

        Assert.Equal(NowUtc, snapshot.RetrievedAtUtc);
    }
}
