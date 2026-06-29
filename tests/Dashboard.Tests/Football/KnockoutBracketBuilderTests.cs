namespace Dashboard.Tests.Football;

public class KnockoutBracketBuilderTests
{
    private static readonly DateTimeOffset Apr28 = new(2026, 4, 28, 19, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset May06 = new(2026, 5, 6, 19, 0, 0, TimeSpan.Zero);

    private static Fixture Leg(
        string stage, int homeId, int awayId, int? hg, int? ag, DateTimeOffset when,
        int? hp = null, int? ap = null, MatchStatus status = MatchStatus.Finished)
        => new(when, stage, null,
            new TeamRef(homeId, $"T{homeId}", $"T{homeId}"),
            new TeamRef(awayId, $"T{awayId}", $"T{awayId}"),
            hg, ag, hp, ap, AfterExtraTime: hp.HasValue, status);

    [Fact]
    public void SingleLeg_DecidedByScore()
    {
        var bracket = KnockoutBracketBuilder.Build([Leg("FINAL", 10, 20, 2, 1, May06)]);

        var tie = Assert.Single(Assert.Single(bracket.Rounds).Ties);
        Assert.False(tie.TwoLegs);
        Assert.True(tie.Decided);
        Assert.Equal(10, tie.Winner!.Value.Id);
        Assert.Equal(2, tie.HomeAggregate);
        Assert.Equal(1, tie.AwayAggregate);
    }

    [Fact]
    public void SingleLeg_DecidedByPenalties()
    {
        // 1:1 nach Verlängerung, 4:3 im Elfmeterschießen → Heim gewinnt.
        var bracket = KnockoutBracketBuilder.Build([Leg("FINAL", 10, 20, 1, 1, May06, hp: 4, ap: 3)]);

        var tie = Assert.Single(Assert.Single(bracket.Rounds).Ties);
        Assert.Equal(10, tie.Winner!.Value.Id);
        Assert.Equal(4, tie.HomePenalties);
        Assert.Equal(3, tie.AwayPenalties);
    }

    [Fact]
    public void TwoLeg_DecidedByAggregate()
    {
        // Leg1: 10 (heim) 5:4 20. Leg2: 20 (heim) 1:1 10. Aggregat aus Sicht von 10: 6:5.
        var bracket = KnockoutBracketBuilder.Build(
        [
            Leg("SEMI_FINALS", 10, 20, 5, 4, Apr28),
            Leg("SEMI_FINALS", 20, 10, 1, 1, May06)
        ]);

        var tie = Assert.Single(Assert.Single(bracket.Rounds).Ties);
        Assert.True(tie.TwoLegs);
        Assert.Equal(6, tie.HomeAggregate);
        Assert.Equal(5, tie.AwayAggregate);
        Assert.Equal(10, tie.Winner!.Value.Id);
    }

    [Fact]
    public void TwoLeg_LevelOnAggregate_DecidedByPenalties()
    {
        // Leg1: 10 1:0 20. Leg2: 20 1:0 10 → Aggregat 1:1; Elfmeterschießen in Leg2 gewinnt 20 mit 4:2.
        var bracket = KnockoutBracketBuilder.Build(
        [
            Leg("SEMI_FINALS", 10, 20, 1, 0, Apr28),
            Leg("SEMI_FINALS", 20, 10, 1, 0, May06, hp: 4, ap: 2)
        ]);

        var tie = Assert.Single(Assert.Single(bracket.Rounds).Ties);
        Assert.Equal(1, tie.HomeAggregate);
        Assert.Equal(1, tie.AwayAggregate);
        // Schießen an die Tie-Orientierung (Heim = 10) angepasst: 10 verliert 2:4.
        Assert.Equal(2, tie.HomePenalties);
        Assert.Equal(4, tie.AwayPenalties);
        Assert.Equal(20, tie.Winner!.Value.Id);
    }

    [Fact]
    public void Undecided_WhenNotAllLegsPlayed()
    {
        var bracket = KnockoutBracketBuilder.Build(
        [
            Leg("SEMI_FINALS", 10, 20, 1, 0, Apr28),
            Leg("SEMI_FINALS", 20, 10, null, null, May06, status: MatchStatus.Scheduled)
        ]);

        var tie = Assert.Single(Assert.Single(bracket.Rounds).Ties);
        Assert.False(tie.Decided);
        Assert.Null(tie.Winner);
        Assert.Null(tie.HomeAggregate);
    }

    [Fact]
    public void SkipsTbdTies_AndOrdersRoundsByStage()
    {
        var bracket = KnockoutBracketBuilder.Build(
        [
            Leg("FINAL", 0, 0, null, null, May06, status: MatchStatus.Scheduled),       // TBD → übersprungen
            Leg("QUARTER_FINALS", 30, 40, 2, 0, Apr28),
            Leg("LAST_16", 10, 20, 1, 0, Apr28),
            Leg("LAST_16", 50, 0, null, null, May06, status: MatchStatus.Scheduled)     // halb-TBD → übersprungen
        ]);

        Assert.Equal(["LAST_16", "QUARTER_FINALS"], bracket.Rounds.Select(r => r.Stage));
        Assert.Single(bracket.Rounds[0].Ties); // nur der bekannte Achtelfinal-Zweikampf
    }
}
