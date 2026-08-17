using Dashboard.Domain.Whoop;
using Dashboard.Infrastructure.Whoop;

namespace Dashboard.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class WhoopStoresIntegrationTests : IAsyncLifetime
{
    private static readonly DateOnly Day = new(2026, 8, 1);
    private static readonly DateTimeOffset When = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _fixture;
    private readonly FakeClock _clock = new() { UtcNow = When };

    public WhoopStoresIntegrationTests(PostgresFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static WhoopDailyMetric FullMetric() => new(
        Day, RecoveryScore: 80, HrvMillis: 55.5, RestingHeartRate: 48,
        SleepHours: 7.5, SleepPerformance: 90, DayStrain: 12.3);

    private static WhoopWorkout Workout(WhoopZoneTimes? zones) => new(
        "workout-1", "running", When, When.AddHours(1),
        DistanceMeters: 8_000, HighIntensityShare: 0.25, Strain: 10.1, Zones: zones);

    [IntegrationFact]
    public async Task Metric_upsert_merges_field_wise_and_keeps_existing_values_on_null()
    {
        var store = new WhoopMetricStore(_fixture.Factory, _clock);
        await store.UpsertAsync([FullMetric()]);

        await store.UpsertAsync([new WhoopDailyMetric(
            Day, RecoveryScore: 95, HrvMillis: null, RestingHeartRate: null,
            SleepHours: null, SleepPerformance: null, DayStrain: null)]);

        var metric = Assert.Single(await store.GetRangeAsync(Day, Day));
        Assert.Equal(95, metric.RecoveryScore);
        Assert.Equal(55.5, metric.HrvMillis);
        Assert.Equal(7.5, metric.SleepHours);
    }

    [IntegrationFact]
    public async Task Oldest_metric_date_is_null_on_an_empty_table()
    {
        var store = new WhoopMetricStore(_fixture.Factory, _clock);

        Assert.Null(await store.GetOldestDateAsync());

        await store.UpsertAsync([FullMetric()]);

        Assert.Equal(Day, await store.GetOldestDateAsync());
    }

    [IntegrationFact]
    public async Task Workout_upsert_fully_replaces_including_clearing_zones()
    {
        var store = new WhoopWorkoutStore(_fixture.Factory, _clock);
        await store.UpsertAsync([Workout(new WhoopZoneTimes(1, 2, 3, 4, 5, 6))]);

        await store.UpsertAsync([Workout(zones: null)]);

        var workout = Assert.Single(await store.GetRangeAsync(When.AddDays(-1), When.AddDays(1)));
        Assert.Null(workout.Zones);
    }

    [IntegrationFact]
    public async Task Marking_a_workout_processed_is_idempotent()
    {
        var store = new WhoopProcessedWorkoutStore(_fixture.Factory);
        await store.MarkProcessedAsync("workout-1", When);
        await store.MarkProcessedAsync("workout-1", When.AddDays(1));

        var processed = await store.GetProcessedAsync(["workout-1", "workout-2"]);

        Assert.Equal(["workout-1"], processed.Order().ToArray());
    }

    [IntegrationFact]
    public async Task Token_store_upserts_a_single_row()
    {
        var store = new WhoopTokenStore(_fixture.Factory);
        Assert.Null(await store.GetAsync());
        Assert.False(await store.HasTokensAsync());

        await store.SaveAsync(new WhoopTokenSet("access-1", "refresh-1", When));
        await store.SaveAsync(new WhoopTokenSet("access-2", "refresh-2", When.AddHours(1)));

        var tokens = await store.GetAsync();
        Assert.NotNull(tokens);
        Assert.Equal("access-2", tokens.AccessToken);
        Assert.True(await store.HasTokensAsync());
    }
}
