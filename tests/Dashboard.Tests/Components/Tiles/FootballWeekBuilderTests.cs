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

    private static Match ClMatch(int day, int hourUtc, string opponent, int? matchday, string? stage) =>
        new(new DateTimeOffset(2026, 6, day, hourUtc, 0, 0, TimeSpan.Zero),
            "CL", opponent, IsHome: true, null, null, matchday, stage);

    [Fact]
    public void Build_CondensesAChampionsLeagueDay_IntoASingleEntry()
    {
        var real = new FootballTeamSnapshot("Real Madrid", [],
            [ClMatch(9, 19, "Arsenal", matchday: 5, stage: "LEAGUE_STAGE")], null);
        var bayern = new FootballTeamSnapshot("Bayern", [],
            [ClMatch(9, 19, "Inter", matchday: 5, stage: "LEAGUE_STAGE")], null);
        var liverpool = new FootballTeamSnapshot("Liverpool", [],
            [ClMatch(9, 17, "Porto", matchday: 5, stage: "LEAGUE_STAGE")], null);

        var tuesday = FootballWeekBuilder.Build(Snapshot(real, bayern, liverpool))[1]; // Di 09.06.

        var entry = Assert.Single(tuesday.Entries);
        Assert.True(entry.IsAggregate);
        Assert.Equal("Champions League · Spieltag 5", entry.Title);
        Assert.Equal("19:00", entry.Time); // frühester Anstoß: 17:00 UTC → 19:00 Berlin
        Assert.DoesNotContain(tuesday.Entries, e => e.TeamName is "Real Madrid" or "Bayern" or "Liverpool");
    }

    [Fact]
    public void Build_KnockoutDay_IsNamedAfterTheRound_NotTheLeg()
    {
        // In der K.o.-Phase zählt matchday nur das Hin-/Rückspiel — „Spieltag 1" wäre falsch.
        var real = new FootballTeamSnapshot("Real Madrid", [],
            [ClMatch(9, 19, "Arsenal", matchday: 1, stage: "LAST_16")], null);

        var entry = Assert.Single(FootballWeekBuilder.Build(Snapshot(real))[1].Entries);

        Assert.Equal("Champions League · Achtelfinale", entry.Title);
    }

    [Fact]
    public void Build_ChampionsLeagueWithoutMatchdayOrStage_FallsBackToTheCompetition()
    {
        var real = new FootballTeamSnapshot("Real Madrid", [],
            [ClMatch(9, 19, "Arsenal", matchday: null, stage: null)], null);

        Assert.Equal("Champions League", FootballWeekBuilder.Build(Snapshot(real))[1].Entries[0].Title);
    }

    [Fact]
    public void Build_LeagueMatchOnAChampionsLeagueDay_StaysItsOwnEntry()
    {
        var real = new FootballTeamSnapshot("Real Madrid", [],
            [ClMatch(9, 19, "Arsenal", matchday: 5, stage: "LEAGUE_STAGE")], null);
        var hsv = new FootballTeamSnapshot("HSV", [], [Match(9, 16, "Bayern", home: false)], null);

        var tuesday = FootballWeekBuilder.Build(Snapshot(real, hsv))[1];

        Assert.Equal(2, tuesday.Entries.Count);
        Assert.Equal("HSV – Bayern", tuesday.Entries[0].Title); // 18:00 vor 21:00
        Assert.True(tuesday.Entries[1].IsAggregate);
    }

    [Fact]
    public void Build_LabelsEveryDay_AndMarksPastDays()
    {
        var week = FootballWeekBuilder.Build(Snapshot());

        Assert.Equal("MO", week[0].DayLabel);
        Assert.Equal("08.06.", week[0].DateLabel);
        Assert.Equal(new DateOnly(2026, 6, 8), week[0].Date);

        Assert.True(week[0].IsPast);       // Mo, vor dem Freitag
        Assert.False(week[4].IsPast);      // Fr, heute
        Assert.True(week[4].IsToday);
        Assert.False(week[6].IsPast);      // So, noch offen
        Assert.False(week[6].IsToday);
    }

    [Fact]
    public void Build_TeamWithoutMatches_LeavesTheOtherTeamsUntouched()
    {
        var hsv = new FootballTeamSnapshot("HSV", [], [Match(10, 12, "Bayern", home: false)], null);
        var silent = new FootballTeamSnapshot("Real Madrid", [], [], null);

        var wednesday = FootballWeekBuilder.Build(Snapshot(hsv, silent))[2];

        Assert.Equal("HSV – Bayern", Assert.Single(wednesday.Entries).Title);
    }
}
