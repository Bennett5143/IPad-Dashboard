namespace Dashboard.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class StravaStateStoresIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset When = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _fixture;

    public StravaStateStoresIntegrationTests(PostgresFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [IntegrationFact]
    public async Task Sync_state_starts_empty_and_a_success_clears_the_last_error()
    {
        var store = new SyncStateStore(_fixture.Factory);

        var empty = await store.GetAsync();
        Assert.Null(empty.LastAttemptUtc);

        await store.RecordFailureAsync("boom", When);
        var failed = await store.GetAsync();
        Assert.Equal("boom", failed.LastError);
        Assert.Null(failed.LastSuccessfulSyncUtc);

        await store.RecordSuccessAsync(When.AddMinutes(5));
        var succeeded = await store.GetAsync();
        Assert.Null(succeeded.LastError);
        Assert.Equal(When.AddMinutes(5), succeeded.LastSuccessfulSyncUtc);
    }

    [IntegrationFact]
    public async Task Details_backfill_marker_round_trips()
    {
        var store = new SyncStateStore(_fixture.Factory);

        await store.MarkDetailsBackfilledAsync(When);

        Assert.Equal(When, (await store.GetAsync()).DetailsBackfilledUtc);
    }

    [IntegrationFact]
    public async Task Strava_token_store_upserts_a_single_row()
    {
        var store = new StravaTokenStore(_fixture.Factory);
        Assert.Null(await store.GetAsync());

        await store.SaveAsync(new StravaTokenSet("access-1", "refresh-1", When));
        await store.SaveAsync(new StravaTokenSet("access-2", "refresh-2", When.AddHours(1)));

        var tokens = await store.GetAsync();
        Assert.NotNull(tokens);
        Assert.Equal("access-2", tokens.AccessToken);
        Assert.Equal("refresh-2", tokens.RefreshToken);
        Assert.True(await store.HasTokensAsync());
    }
}
