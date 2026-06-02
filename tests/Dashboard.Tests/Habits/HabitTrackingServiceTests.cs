namespace Dashboard.Tests.Habits;

public class HabitTrackingServiceTests
{
    private static HabitTrackingService BuildService(
        FakeHabitEntryRepository repo, DateTimeOffset utcNow)
        => new(repo, new FakeClock { UtcNow = utcNow });

    [Fact]
    public async Task GetSummary_FillsZeroCountsForUnusedHabits()
    {
        var repo = new FakeHabitEntryRepository();
        var service = BuildService(repo, new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));

        var summaries = await service.GetSummaryForAsync(new DateOnly(2026, 5, 20));

        Assert.Equal(Enum.GetValues<HabitKind>().Length, summaries.Count);
        Assert.All(summaries, s => Assert.False(s.IsDoneToday));
        Assert.All(summaries, s => Assert.Equal(0, s.WeekCount));
    }

    [Fact]
    public async Task Toggle_AddsEntry_WhenNoneExists()
    {
        var repo = new FakeHabitEntryRepository();
        var service = BuildService(repo, new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var today = new DateOnly(2026, 5, 20);

        await service.ToggleAsync(today, HabitKind.Strength);

        var summaries = await service.GetSummaryForAsync(today);
        var strength = summaries.Single(s => s.Kind == HabitKind.Strength);
        Assert.True(strength.IsDoneToday);
        Assert.Equal(1, strength.WeekCount);
    }

    [Fact]
    public async Task Toggle_RemovesEntry_WhenAlreadyExists()
    {
        var repo = new FakeHabitEntryRepository();
        var service = BuildService(repo, new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var today = new DateOnly(2026, 5, 20);

        await service.ToggleAsync(today, HabitKind.Strength);
        await service.ToggleAsync(today, HabitKind.Strength); // wieder zurück

        var summaries = await service.GetSummaryForAsync(today);
        var strength = summaries.Single(s => s.Kind == HabitKind.Strength);
        Assert.False(strength.IsDoneToday);
        Assert.Equal(0, strength.WeekCount);
    }

    [Fact]
    public async Task GetSummary_CountsOnlyWithinWeek()
    {
        var repo = new FakeHabitEntryRepository();
        await repo.AddAsync(new HabitEntry { Date = new DateOnly(2026, 5, 18), Kind = HabitKind.Strength }); // Mo
        await repo.AddAsync(new HabitEntry { Date = new DateOnly(2026, 5, 24), Kind = HabitKind.Strength }); // So
        await repo.AddAsync(new HabitEntry { Date = new DateOnly(2026, 5, 25), Kind = HabitKind.Strength }); // Mo (nächste Woche)

        var service = BuildService(repo, new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var summaries = await service.GetSummaryForAsync(new DateOnly(2026, 5, 20));

        var strength = summaries.Single(s => s.Kind == HabitKind.Strength);
        Assert.Equal(2, strength.WeekCount); // nur Mo + So dieser Woche
        Assert.Equal(3, strength.YearCount); // alle drei im selben Kalenderjahr
    }

    [Fact]
    public async Task SaveEmom_ThrowsOnInvalidSegments()
    {
        var service = BuildService(new FakeHabitEntryRepository(),
            new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));

        var badSegments = new[] { new EmomSegment { FromMinute = 2, ToMinute = 8 } }; 

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveEmomAsync(new DateOnly(2026, 5, 20), badSegments));
    }

    [Fact]
    public async Task SaveEmom_MarksGymAsDoneAndCountsIt()
    {
        var repo = new FakeHabitEntryRepository();
        var service = BuildService(repo, new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var today = new DateOnly(2026, 5, 20);

        await service.SaveEmomAsync(today, new[]
        {
            new EmomSegment { FromMinute = 1, ToMinute = 10, PullupsPerMinute = 8, PushupsPerMinute = 4 }
        });

        var summaries = await service.GetSummaryForAsync(today);
        var gym = summaries.Single(s => s.Kind == HabitKind.Strength);
        Assert.True(gym.IsDoneToday);
        Assert.Equal(1, gym.WeekCount);
        Assert.NotNull(gym.TodaysEmom);
        Assert.Equal(80, gym.TodaysEmom!.TotalPullups);
    }

    [Fact]
    public async Task SaveRunning_RejectsNonRunningKind()
    {
        var service = BuildService(new FakeHabitEntryRepository(),
            new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveRunningAsync(new DateOnly(2026, 5, 20), HabitKind.Strength, 30, 5.5m));
    }

    [Fact]
    public async Task SaveRunning_MarksDoneAndStoresDetails()
    {
        var repo = new FakeHabitEntryRepository();
        var service = BuildService(repo, new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var today = new DateOnly(2026, 5, 20);

        await service.SaveRunningAsync(today, HabitKind.Zone2Run, 45, 5.30m);

        var z2 = (await service.GetSummaryForAsync(today)).Single(s => s.Kind == HabitKind.Zone2Run);
        Assert.True(z2.IsDoneToday);
        Assert.Equal(1, z2.WeekCount);
        Assert.NotNull(z2.TodaysRunning);
        Assert.Equal(45, z2.TodaysRunning!.DurationMinutes);
    }
}