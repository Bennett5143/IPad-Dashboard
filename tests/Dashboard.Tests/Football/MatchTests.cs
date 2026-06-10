namespace Dashboard.Tests.Football;

public class MatchTests
{
    private static Match Played(int own, int opp) =>
        new(new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero), "PD", "Gegner", true, own, opp);

    [Fact]
    public void Outcome_IsWin_WhenOwnGoalsHigher()
    {
        Assert.Equal(MatchOutcome.Win, Played(2, 1).Outcome);
    }

    [Fact]
    public void Outcome_IsDraw_WhenEqual()
    {
        Assert.Equal(MatchOutcome.Draw, Played(1, 1).Outcome);
    }

    [Fact]
    public void Outcome_IsLoss_WhenOwnGoalsLower()
    {
        Assert.Equal(MatchOutcome.Loss, Played(0, 2).Outcome);
    }

    [Fact]
    public void Outcome_IsNull_WhenNotFinished()
    {
        var fixture = new Match(
            new DateTimeOffset(2026, 6, 1, 19, 0, 0, TimeSpan.Zero), "PD", "Gegner", false, null, null);

        Assert.False(fixture.IsFinished);
        Assert.Null(fixture.Outcome);
    }
}
