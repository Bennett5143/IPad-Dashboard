namespace Dashboard.Tests.Football;

public class FootballStateTests
{
    private static FootballSnapshot Snapshot(DateTimeOffset retrievedAt) =>
        new([new FootballTeamSnapshot("Real Madrid", [], [], null)], retrievedAt);

    [Fact]
    public void Update_StoresSnapshotAndRaisesChanged()
    {
        var state = new FootballState();
        var raised = 0;
        state.Changed += () => raised++;

        var snapshot = Snapshot(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero));
        state.Update(snapshot);

        Assert.Same(snapshot, state.Current);
        Assert.False(state.IsStale);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void MarkStale_WithoutData_IsNoOp()
    {
        var state = new FootballState();
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
        var state = new FootballState();
        var snapshot = Snapshot(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero));
        state.Update(snapshot);

        var raised = 0;
        state.Changed += () => raised++;
        state.MarkStale();

        Assert.True(state.IsStale);
        Assert.Same(snapshot, state.Current);
        Assert.Equal(1, raised);
    }
}
