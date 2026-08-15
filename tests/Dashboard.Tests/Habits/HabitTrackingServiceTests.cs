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

        Assert.Equal(HabitCatalog.Active.Count, summaries.Count);
        Assert.All(summaries, s => Assert.False(s.IsDoneToday));
        Assert.All(summaries, s => Assert.Equal(0, s.WeekCount));
    }

    /// <summary>
    /// Der Tracker führt genau die aktiven Gewohnheiten. Abgewählte bleiben im Enum, weil ihre
    /// Zeilen als String in der Datenbank liegen und lesbar bleiben müssen — sichtbar sind sie nicht.
    /// </summary>
    [Fact]
    public async Task GetSummary_ListsOnlyTheActiveHabits()
    {
        var repo = new FakeHabitEntryRepository();
        var service = BuildService(repo, new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));

        var summaries = await service.GetSummaryForAsync(new DateOnly(2026, 5, 20));

        Assert.Equal(
            [HabitKind.Strength, HabitKind.Zone2Run, HabitKind.Vo2MaxIntervals],
            summaries.Select(s => s.Kind));
        Assert.DoesNotContain(summaries, s => s.Kind is HabitKind.JumpRope or HabitKind.Stretching);
    }

    /// <summary>
    /// Historie einer abgewählten Gewohnheit: sie lädt fehlerfrei und taucht in keiner Zusammen-
    /// fassung auf. Ohne diese Zusicherung bräuchte das Abwählen eine Datenwanderung.
    /// </summary>
    [Fact]
    public async Task GetSummary_IgnoresEntriesOfDroppedHabits()
    {
        var date = new DateOnly(2026, 5, 20);
        var repo = new FakeHabitEntryRepository();
        await repo.AddAsync(new HabitEntry { Date = date, Kind = HabitKind.JumpRope });
        await repo.AddAsync(new HabitEntry { Date = date, Kind = HabitKind.Strength });

        var service = BuildService(repo, new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var summaries = await service.GetSummaryForAsync(date);

        Assert.Equal(3, summaries.Count);
        Assert.True(summaries.Single(s => s.Kind == HabitKind.Strength).IsDoneToday);
        Assert.DoesNotContain(summaries, s => s.Kind == HabitKind.JumpRope);
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

    [Fact]
    public async Task SaveRunning_BackdatedDate_StoresDetailsForThatDateOnly()
    {
        // Nachtragen eines Laufs mit Details für ein vergangenes Datum.
        var repo = new FakeHabitEntryRepository();
        var service = BuildService(repo, new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var pastDate = new DateOnly(2026, 5, 10);

        await service.SaveRunningAsync(pastDate, HabitKind.Vo2MaxIntervals, 28, 4.15m);

        var past = (await service.GetSummaryForAsync(pastDate)).Single(s => s.Kind == HabitKind.Vo2MaxIntervals);
        Assert.True(past.IsDoneToday);
        Assert.NotNull(past.TodaysRunning);
        Assert.Equal(28, past.TodaysRunning!.DurationMinutes);

        // Der heutige Tag bleibt davon unberührt.
        var today = (await service.GetSummaryForAsync(new DateOnly(2026, 5, 20))).Single(s => s.Kind == HabitKind.Vo2MaxIntervals);
        Assert.False(today.IsDoneToday);
    }

    [Fact]
    public async Task SaveEmom_BackdatedDate_StoresDetailsForThatDate()
    {
        // Nachtragen eines Gym-/EMOM-Workouts mit Details für ein vergangenes Datum.
        var repo = new FakeHabitEntryRepository();
        var service = BuildService(repo, new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var pastDate = new DateOnly(2026, 5, 8);

        await service.SaveEmomAsync(pastDate, new[]
        {
            new EmomSegment { FromMinute = 1, ToMinute = 12, PullupsPerMinute = 6, PushupsPerMinute = 6 }
        });

        var gym = (await service.GetSummaryForAsync(pastDate)).Single(s => s.Kind == HabitKind.Strength);
        Assert.True(gym.IsDoneToday);
        Assert.NotNull(gym.TodaysEmom);
    }
}
