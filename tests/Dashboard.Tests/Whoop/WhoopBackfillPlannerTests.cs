using Dashboard.Domain.Whoop;

namespace Dashboard.Tests.Whoop;

public class WhoopBackfillPlannerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NextWindow_StepsBackwards_ByWindowSize()
    {
        var before = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var window = WhoopBackfillPlanner.NextWindow(NowUtc, before, backfillDays: 365, windowDays: 90);

        Assert.NotNull(window);
        Assert.Equal(before, window!.ToUtc);
        Assert.Equal(before.AddDays(-90), window.FromUtc);
    }

    [Fact]
    public void NextWindow_ClampsToConfiguredDepth()
    {
        var floor = NowUtc.AddDays(-365);
        var before = floor.AddDays(30); // nur noch 30 Tage bis zur Tiefen-Grenze

        var window = WhoopBackfillPlanner.NextWindow(NowUtc, before, backfillDays: 365, windowDays: 90);

        Assert.Equal(floor, window!.FromUtc);
        Assert.Equal(before, window.ToUtc);
    }

    [Fact]
    public void NextWindow_ReturnsNull_WhenDepthReached()
    {
        var floor = NowUtc.AddDays(-365);

        Assert.Null(WhoopBackfillPlanner.NextWindow(NowUtc, floor, backfillDays: 365, windowDays: 90));
        Assert.Null(WhoopBackfillPlanner.NextWindow(NowUtc, floor.AddDays(-5), backfillDays: 365, windowDays: 90));
    }

    [Fact]
    public void NextWindow_ReturnsNull_WhenDisabled()
    {
        var before = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.Null(WhoopBackfillPlanner.NextWindow(NowUtc, before, backfillDays: 0, windowDays: 90));
        Assert.Null(WhoopBackfillPlanner.NextWindow(NowUtc, before, backfillDays: 365, windowDays: 0));
    }

    [Fact]
    public void StartOfBerlinDay_UsesSummerAndWinterOffset()
    {
        // CEST (+02:00) im Juni …
        Assert.Equal(
            new DateTimeOffset(2026, 6, 11, 0, 0, 0, TimeSpan.FromHours(2)),
            WhoopBackfillPlanner.StartOfBerlinDay(new DateOnly(2026, 6, 11)));

        // … CET (+01:00) im Januar.
        Assert.Equal(
            new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.FromHours(1)),
            WhoopBackfillPlanner.StartOfBerlinDay(new DateOnly(2026, 1, 15)));
    }
}
