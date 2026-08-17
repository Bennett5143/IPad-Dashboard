namespace Dashboard.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RunPlaceStoreIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset When = new(2026, 8, 1, 7, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _fixture;
    private readonly RunRepository _runRepository;
    private readonly RunPlaceStore _store;

    public RunPlaceStoreIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _runRepository = new RunRepository(fixture.Factory);
        _store = new RunPlaceStore(fixture.Factory);
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly GeoPoint[] Track =
    [
        new(53.5500, 10.0000),
        new(53.5510, 10.0010),
        new(53.5520, 10.0020),
    ];

    private Task AddRunAsync(long id) => _runRepository.UpsertAsync(
        [new Run(id, $"Run {id}", "Run", When.AddDays(id), 5_000, TimeSpan.FromMinutes(30), Track)]);

    [IntegrationFact]
    public async Task Creating_a_place_assigns_the_run_and_counts_it()
    {
        await AddRunAsync(1);

        var placeId = await _store.CreatePlaceAsync(1, Track, When);

        var place = Assert.Single(await _store.GetPlacesAsync());
        Assert.Equal(placeId, place.Id);
        Assert.Equal(1, place.RunCount);

        var info = await _store.GetPlaceForRunAsync(1);
        Assert.NotNull(info);
        Assert.Equal(placeId, info.Id);
        Assert.Empty(await _store.GetUnassignedRunIdsAsync(10));
    }

    [IntegrationFact]
    public async Task Assigning_a_second_run_increments_the_count_and_links_both_runs()
    {
        await AddRunAsync(1);
        await AddRunAsync(2);
        var placeId = await _store.CreatePlaceAsync(1, Track, When);

        await _store.AssignAsync(2, placeId, Track, When);

        var place = Assert.Single(await _store.GetPlacesAsync());
        Assert.Equal(2, place.RunCount);
        Assert.Equal(new long[] { 1, 2 }, (await _store.GetRunIdsForPlaceAsync(placeId)).Order().ToArray());
    }

    [IntegrationFact]
    public async Task Unassigned_run_ids_come_back_oldest_first_and_skip_marked_runs()
    {
        await AddRunAsync(2);
        await AddRunAsync(1);

        Assert.Equal(new long[] { 1, 2 }, (await _store.GetUnassignedRunIdsAsync(10)).ToArray());

        await _store.MarkUnassignableAsync(1, When);

        Assert.Equal(new long[] { 2 }, (await _store.GetUnassignedRunIdsAsync(10)).ToArray());
        Assert.Null(await _store.GetPlaceForRunAsync(1));
    }

    [IntegrationFact]
    public async Task Renaming_truncates_to_eighty_characters()
    {
        await AddRunAsync(1);
        var placeId = await _store.CreatePlaceAsync(1, Track, When);

        await _store.RenameAsync(placeId, new string('x', 120));

        var place = Assert.Single(await _store.GetPlacesAsync());
        Assert.Equal(80, place.Name.Length);
    }

    [IntegrationFact]
    public async Task Summaries_aggregate_distance_and_pace_per_place()
    {
        await AddRunAsync(1);
        await AddRunAsync(2);
        var placeId = await _store.CreatePlaceAsync(1, Track, When);
        await _store.AssignAsync(2, placeId, Track, When);

        var summary = Assert.Single(await _store.GetSummariesAsync());

        Assert.Equal(2, summary.RunCount);
        Assert.Equal(10.0, summary.TotalDistanceKm, precision: 3);
        Assert.NotNull(summary.AveragePaceMinPerKm);
        Assert.Equal(6.0, summary.AveragePaceMinPerKm.Value, precision: 3);
    }
}
