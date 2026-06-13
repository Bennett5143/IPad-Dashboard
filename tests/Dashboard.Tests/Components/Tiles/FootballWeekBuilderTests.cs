using Dashboard.Web.Components.Tiles;

namespace Dashboard.Tests.Components.Tiles;

public class FootballWeekBuilderTests
{
    // Snapshot „abgerufen" am Freitag, 12.06.2026, 10:00 UTC → Berlin-Woche Mo 08. – So 14.06.
    private static readonly DateTimeOffset RetrievedUtc = new(2026, 6, 12, 10, 0, 0, TimeSpan.Zero);

    private static Match Match(int day, int hourUtc, string opponent, bool home, int? own = null, int? opp = null) =>
        new(new DateTimeOffset(2026, 6, day, hourUtc, 0, 0, TimeSpan.Zero), "BL1", opponent, home, own, opp);

    private static FootballSnapshot Snapshot(params FootballTeamSnapshot[] teams) =>
        new(teams, RetrievedUtc);

    [Fact]
    public void Build_ReturnsSevenDays_MondayToSunday()
    {
        var week = FootballWeekBuilder.Build(Snapshot());

        Assert.Equal(7, week.Count);
        Assert.StartsWith("Mo 08.06.", week[0].Label, StringComparison.Ordinal);
        Assert.StartsWith("So 14.06.", week[6].Label, StringComparison.Ordinal);
        Assert.False(FootballWeekBuilder.HasMatches(week));
    }

    [Fact]
    public void Build_PlacesMatchesOnCorrectDay_AndMarksToday()
    {
        var team = new FootballTeamSnapshot("HSV",
            RecentResults: [Match(8, 13, "St. Pauli", home: true, own: 2, opp: 1)],   // Mo, beendet
            Upcoming: [Match(12, 17, "Bayern", home: false)],                          // Fr (heute)
            Standing: null);

        var week = FootballWeekBuilder.Build(Snapshot(team));

        Assert.True(FootballWeekBuilder.HasMatches(week));

        var monday = week[0];
        var entry = Assert.Single(monday.Entries);
        Assert.Equal("HSV", entry.TeamName);
        Assert.Equal("St. Pauli", entry.Opponent);
        Assert.Equal("H", entry.Venue);
        Assert.True(entry.IsFinished);
        Assert.Equal("2:1", entry.Result);

        var friday = week[4];
        Assert.True(friday.IsToday);
        var upcoming = Assert.Single(friday.Entries);
        Assert.Equal("A", upcoming.Venue);
        Assert.False(upcoming.IsFinished);
        Assert.Null(upcoming.Result);
        Assert.Equal("19:00", upcoming.Time); // 17:00 UTC → 19:00 Berlin (CEST)
    }

    [Fact]
    public void Build_IgnoresMatchesOutsideTheWeek()
    {
        var team = new FootballTeamSnapshot("HSV",
            RecentResults: [Match(1, 13, "Kiel", home: true, own: 1, opp: 1)], // 01.06. (Vorwoche)
            Upcoming: [Match(20, 13, "Bremen", home: true)],                   // 20.06. (Folgewoche)
            Standing: null);

        Assert.False(FootballWeekBuilder.HasMatches(FootballWeekBuilder.Build(Snapshot(team))));
    }

    [Fact]
    public void Build_MergesMatchesFromBothTeams_SortedByTime()
    {
        var real = new FootballTeamSnapshot("Real Madrid", [], [Match(10, 19, "Sevilla", home: true)], null);
        var hsv = new FootballTeamSnapshot("HSV", [], [Match(10, 12, "Bayern", home: false)], null);

        var wednesday = FootballWeekBuilder.Build(Snapshot(real, hsv))[2]; // Mi 10.06.

        Assert.Equal(2, wednesday.Entries.Count);
        Assert.Equal("HSV", wednesday.Entries[0].TeamName);          // 12:00 vor 19:00
        Assert.Equal("Real Madrid", wednesday.Entries[1].TeamName);
    }
}
