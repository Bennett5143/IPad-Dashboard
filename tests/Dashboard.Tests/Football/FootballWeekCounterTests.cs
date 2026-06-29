namespace Dashboard.Tests.Football;

public class FootballWeekCounterTests
{
    // Mi 2026-06-10 12:00 UTC → Woche Mo 08.06. – So 14.06. (Europe/Berlin).
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset InWeek = new(2026, 6, 13, 18, 30, 0, TimeSpan.Zero);   // Sa
    private static readonly DateTimeOffset OutWeek = new(2026, 6, 16, 18, 30, 0, TimeSpan.Zero);  // Di danach

    [Fact]
    public void Empty_ReturnsZero() =>
        Assert.Equal(0, FootballWeekCounter.CountInterestingGames([], NowUtc));

    [Fact]
    public void CountsFixtureWithinCurrentWeek()
    {
        FixtureKey[] fixtures = [new("PD", InWeek, 86, 81)];

        Assert.Equal(1, FootballWeekCounter.CountInterestingGames(fixtures, NowUtc));
    }

    [Fact]
    public void IgnoresFixtureOutsideCurrentWeek()
    {
        FixtureKey[] fixtures = [new("PD", OutWeek, 86, 81)];

        Assert.Equal(0, FootballWeekCounter.CountInterestingGames(fixtures, NowUtc));
    }

    [Fact]
    public void DeduplicatesSameFixtureSeenFromBothTeams()
    {
        // Dasselbe Spiel zweier getrackter Vereine, aus beiden Perspektiven geliefert
        // (Heim/Auswärts vertauscht) → darf nur einmal zählen.
        FixtureKey[] fixtures =
        [
            new("PD", InWeek, 86, 81),
            new("PD", InWeek, 81, 86)
        ];

        Assert.Equal(1, FootballWeekCounter.CountInterestingGames(fixtures, NowUtc));
    }

    [Fact]
    public void CountsDistinctSimultaneousFixturesSeparately()
    {
        // Zwei verschiedene Spiele zur selben Anstoßzeit im selben Wettbewerb (anderes Team-Paar)
        // → kein Über-Dedup, beide zählen.
        FixtureKey[] fixtures =
        [
            new("PL", InWeek, 64, 57),
            new("PL", InWeek, 65, 66)
        ];

        Assert.Equal(2, FootballWeekCounter.CountInterestingGames(fixtures, NowUtc));
    }
}
