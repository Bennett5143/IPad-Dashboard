namespace Dashboard.Tests.Weather;

public class WeatherStateTests
{
    private static WeatherSnapshot Snapshot(DateTimeOffset retrievedAt) => new(
        new CurrentWeather(15, 14, WeatherCondition.Clear, "Klar"),
        new DailyForecast(new DateOnly(2026, 6, 10), 12, 18, WeatherCondition.Clear, "Klar", 0.1),
        Tomorrow: null,
        Hourly: Array.Empty<HourlyForecast>(),
        retrievedAt);

    [Fact]
    public void Update_StoresSnapshotAndRaisesChanged()
    {
        var state = new WeatherState();
        var raised = 0;
        state.Changed += () => raised++;

        var snapshot = Snapshot(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero));
        state.Update(snapshot);

        Assert.Same(snapshot, state.Current);
        Assert.False(state.IsStale);
        Assert.Equal(snapshot.RetrievedAtUtc, state.LastUpdatedUtc);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void MarkStale_WithoutData_IsNoOp()
    {
        var state = new WeatherState();
        var raised = 0;
        state.Changed += () => raised++;

        state.MarkStale();

        Assert.False(state.IsStale);
        Assert.Null(state.Current);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void MarkStale_WithData_FlagsStaleAndKeepsData()
    {
        var state = new WeatherState();
        var snapshot = Snapshot(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero));
        state.Update(snapshot);

        var raised = 0;
        state.Changed += () => raised++;
        state.MarkStale();

        Assert.True(state.IsStale);
        Assert.Same(snapshot, state.Current);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Update_AfterStale_ClearsStaleFlag()
    {
        var state = new WeatherState();
        state.Update(Snapshot(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero)));
        state.MarkStale();

        state.Update(Snapshot(new DateTimeOffset(2026, 6, 10, 12, 15, 0, TimeSpan.Zero)));

        Assert.False(state.IsStale);
    }
}
