using Dashboard.Infrastructure.Habits;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class HabitEntryRepositoryIntegrationTests : IAsyncLifetime
{
    private static readonly DateOnly Day = new(2026, 8, 1);

    private readonly PostgresFixture _fixture;
    private readonly HabitEntryRepository _repository;

    public HabitEntryRepositoryIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _repository = new HabitEntryRepository(fixture.Factory);
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [IntegrationFact]
    public async Task Add_get_and_remove_round_trip()
    {
        await _repository.AddAsync(new HabitEntry { Date = Day, Kind = HabitKind.JumpRope });

        var entry = await _repository.GetAsync(Day, HabitKind.JumpRope);
        Assert.NotNull(entry);
        Assert.Contains(HabitKind.JumpRope, await _repository.GetCompletedKindsAsync(Day));

        await _repository.RemoveAsync(entry);

        Assert.Null(await _repository.GetAsync(Day, HabitKind.JumpRope));
    }

    [IntegrationFact]
    public async Task The_unique_index_rejects_a_second_entry_for_the_same_day_and_kind()
    {
        await _repository.AddAsync(new HabitEntry { Date = Day, Kind = HabitKind.Stretching });

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _repository.AddAsync(new HabitEntry { Date = Day, Kind = HabitKind.Stretching }));
    }

    [IntegrationFact]
    public async Task Upserting_running_details_creates_then_overwrites()
    {
        await _repository.UpsertRunningAsync(Day, HabitKind.Zone2Run, new RunningDetails(45, 6.30m));
        await _repository.UpsertRunningAsync(Day, HabitKind.Zone2Run, new RunningDetails(50, 6.10m));

        var running = await _repository.GetRunningForDateAsync(Day);

        Assert.Equal(new RunningDetails(50, 6.10m), running[HabitKind.Zone2Run]);
    }

    [IntegrationFact]
    public async Task Upserting_an_emom_replaces_the_previous_segments()
    {
        await _repository.UpsertEmomAsync(Day, [new EmomSegment { FromMinute = 0, ToMinute = 9, PushupsPerMinute = 10, PullupsPerMinute = 2 }]);
        await _repository.UpsertEmomAsync(Day, [
            new EmomSegment { FromMinute = 0, ToMinute = 4, PushupsPerMinute = 12, PullupsPerMinute = 3 },
            new EmomSegment { FromMinute = 5, ToMinute = 9, PushupsPerMinute = 8, PullupsPerMinute = 2 },
        ]);

        var emom = await _repository.GetEmomAsync(Day);

        Assert.NotNull(emom);
        Assert.Equal(2, emom.Segments.Count);
        Assert.Equal(12, emom.Segments.OrderBy(s => s.FromMinute).First().PushupsPerMinute);
    }

    [IntegrationFact]
    public async Task Entry_dates_and_counts_aggregate_over_a_range()
    {
        await _repository.AddAsync(new HabitEntry { Date = Day, Kind = HabitKind.JumpRope });
        await _repository.AddAsync(new HabitEntry { Date = Day.AddDays(1), Kind = HabitKind.JumpRope });

        var counts = await _repository.CountByKindAsync(Day, Day.AddDays(7));
        Assert.Equal(2, counts[HabitKind.JumpRope]);

        var dates = await _repository.GetEntryDatesAsync(Day, Day.AddDays(7));
        Assert.Equal([Day, Day.AddDays(1)], dates[HabitKind.JumpRope].Order().ToArray());
    }
}
