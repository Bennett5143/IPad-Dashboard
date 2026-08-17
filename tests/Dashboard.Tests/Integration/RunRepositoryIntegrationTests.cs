namespace Dashboard.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RunRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly RunRepository _repository;

    public RunRepositoryIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _repository = new RunRepository(fixture.Factory);
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly GeoPoint[] Track =
    [
        new(53.5500, 10.0000),
        new(53.5510, 10.0010),
        new(53.5520, 10.0020),
    ];

    private static Run MakeRun(long id, IReadOnlyList<GeoPoint>? track = null, string name = "Morning Run") =>
        new(id, name, "Run", new DateTimeOffset(2026, 8, 1, 6, 0, 0, TimeSpan.Zero),
            DistanceMeters: 5_000, MovingTime: TimeSpan.FromMinutes(30), Track: track ?? Track);

    [IntegrationFact]
    public async Task Upsert_round_trips_the_route_geometry()
    {
        await _repository.UpsertAsync([MakeRun(1)]);

        var run = Assert.Single(await _repository.GetRunsAsync(null));

        Assert.Equal(1, run.Id);
        Assert.Equal(Track, run.Track);
    }

    [IntegrationFact]
    public async Task Upsert_with_the_same_id_updates_instead_of_duplicating()
    {
        await _repository.UpsertAsync([MakeRun(1)]);
        await _repository.UpsertAsync([MakeRun(1, name: "Renamed Run")]);

        var run = Assert.Single(await _repository.GetRunsAsync(null));

        Assert.Equal("Renamed Run", run.Name);
        Assert.Equal(1, await _repository.CountAsync());
    }

    [IntegrationFact]
    public async Task A_backfilled_stream_route_survives_later_summary_upserts()
    {
        var coarse = new[] { new GeoPoint(53.55, 10.0), new GeoPoint(53.56, 10.01) };
        var detailed = new[]
        {
            new GeoPoint(53.5500, 10.0000),
            new GeoPoint(53.5505, 10.0005),
            new GeoPoint(53.5510, 10.0010),
            new GeoPoint(53.5515, 10.0015),
        };
        await _repository.UpsertAsync([MakeRun(1, coarse)]);

        await _repository.SaveStreamsAsync(1, new StravaStreams(
            detailed, TimeOffsetsSeconds: [0, 60, 120, 180], AltitudesMeters: null, HeartRates: null));
        await _repository.UpsertAsync([MakeRun(1, coarse)]);

        var run = await _repository.GetRunAsync(1);

        Assert.NotNull(run);
        Assert.Equal(detailed, run.Track);
        Assert.NotNull(run.Streams);
        Assert.Empty(await _repository.GetIdsMissingStreamsAsync(10));
    }

    [IntegrationFact]
    public async Task Summaries_come_back_without_the_track()
    {
        await _repository.UpsertAsync([MakeRun(1)]);

        var summary = Assert.Single(await _repository.GetRunSummariesAsync(null));

        Assert.Empty(summary.Track);
        Assert.Equal(5_000, summary.DistanceMeters);
    }

    [IntegrationFact]
    public async Task Latest_run_start_is_null_on_an_empty_table_and_the_max_otherwise()
    {
        Assert.Null(await _repository.GetLatestRunStartAsync());

        await _repository.UpsertAsync([MakeRun(1)]);

        Assert.Equal(MakeRun(1).StartUtc, await _repository.GetLatestRunStartAsync());
    }
}
