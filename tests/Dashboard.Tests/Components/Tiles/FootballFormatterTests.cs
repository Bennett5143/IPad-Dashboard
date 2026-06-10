namespace Dashboard.Tests.Components.Tiles;

public class FootballFormatterTests
{
    private static Match Finished(int own, int opp, bool home = true) =>
        new(new DateTimeOffset(2026, 5, 24, 13, 0, 0, TimeSpan.Zero), "PD", "Barça", home, own, opp);

    [Fact]
    public void ShortDate_IsGermanWeekdayAndDayMonth()
    {
        // 24. Mai 2026 ist ein Sonntag; 13:00 UTC = 15:00 Berlin
        var utc = new DateTimeOffset(2026, 5, 24, 13, 0, 0, TimeSpan.Zero);

        Assert.Equal("So 24.05.", FootballFormatter.ShortDate(utc));
    }

    [Fact]
    public void DateTimeLabel_IncludesBerlinKickoffTime()
    {
        // 24. Mai 2026, 13:00 UTC = 15:00 Berlin (Sommerzeit)
        var utc = new DateTimeOffset(2026, 5, 24, 13, 0, 0, TimeSpan.Zero);

        Assert.Equal("So 24.05. 15:00", FootballFormatter.DateTimeLabel(utc));
    }

    [Theory]
    [InlineData("PD", "La Liga")]
    [InlineData("CL", "Champions League")]
    [InlineData("BL1", "Bundesliga")]
    [InlineData("XYZ", "XYZ")]
    public void Competition_MapsCodeToFriendlyName(string code, string expected)
    {
        Assert.Equal(expected, FootballFormatter.Competition(code));
    }

    [Fact]
    public void Score_ShowsOwnVersusOpponent()
    {
        Assert.Equal("2:1", FootballFormatter.Score(Finished(2, 1)));
    }

    [Fact]
    public void Score_ShowsDash_WhenNotFinished()
    {
        var fixture = new Match(
            new DateTimeOffset(2026, 6, 1, 19, 0, 0, TimeSpan.Zero), "PD", "Valencia", true, null, null);

        Assert.Equal("–", FootballFormatter.Score(fixture));
    }

    [Theory]
    [InlineData(MatchOutcome.Win, "S")]
    [InlineData(MatchOutcome.Draw, "U")]
    [InlineData(MatchOutcome.Loss, "N")]
    public void OutcomeLabel_MapsToGermanLetters(MatchOutcome outcome, string expected)
    {
        Assert.Equal(expected, FootballFormatter.OutcomeLabel(outcome));
    }

    [Fact]
    public void Position_ShowsDash_WhenNoStanding()
    {
        Assert.Equal("–", FootballFormatter.Position(null));
        Assert.Equal("3.", FootballFormatter.Position(new TablePosition(3, 30, 60)));
    }

    [Fact]
    public void Venue_DistinguishesHomeAndAway()
    {
        Assert.Equal("H", FootballFormatter.Venue(Finished(1, 0, home: true)));
        Assert.Equal("A", FootballFormatter.Venue(Finished(1, 0, home: false)));
    }
}
