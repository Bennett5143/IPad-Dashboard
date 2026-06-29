using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Football;

public class FootballCupAssemblyTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 29, 12, 0, 0, TimeSpan.Zero);

    private const string ClStandings =
        """
        { "competition": { "name": "UEFA Champions League", "code": "CL" },
          "standings": [ { "type": "TOTAL", "group": "League phase", "table": [
            { "position": 1, "team": { "id": 524, "name": "Paris Saint-Germain", "tla": "PSG" }, "playedGames": 8, "won": 7, "draw": 1, "lost": 0, "goalDifference": 15, "points": 22 },
            { "position": 2, "team": { "id": 81, "name": "FC Barcelona", "tla": "FCB" }, "playedGames": 8, "won": 6, "draw": 1, "lost": 1, "goalDifference": 12, "points": 19 }
          ] } ] }
        """;

    private const string ClMatches =
        """
        { "matches": [
          { "utcDate": "2026-04-28T19:00:00Z", "status": "FINISHED", "stage": "SEMI_FINALS", "group": null,
            "competition": { "name": "UEFA Champions League", "code": "CL" },
            "homeTeam": { "id": 524, "name": "Paris Saint-Germain", "tla": "PSG" },
            "awayTeam": { "id": 81, "name": "FC Barcelona", "tla": "FCB" },
            "score": { "winner": "HOME_TEAM", "duration": "REGULAR", "fullTime": { "home": 5, "away": 4 } } },
          { "utcDate": "2026-05-06T19:00:00Z", "status": "FINISHED", "stage": "SEMI_FINALS", "group": null,
            "competition": { "name": "UEFA Champions League", "code": "CL" },
            "homeTeam": { "id": 81, "name": "FC Barcelona", "tla": "FCB" },
            "awayTeam": { "id": 524, "name": "Paris Saint-Germain", "tla": "PSG" },
            "score": { "winner": "DRAW", "duration": "REGULAR", "fullTime": { "home": 1, "away": 1 } } }
        ] }
        """;

    private const string WcStandings =
        """
        { "competition": { "name": "FIFA World Cup", "code": "WC" },
          "standings": [
            { "type": "TOTAL", "group": "Group A", "table": [
              { "position": 1, "team": { "id": 770, "name": "USA", "tla": "USA" }, "playedGames": 3, "won": 3, "draw": 0, "lost": 0, "goalDifference": 6, "points": 9 },
              { "position": 2, "team": { "id": 771, "name": "Mexico", "tla": "MEX" }, "playedGames": 3, "won": 1, "draw": 1, "lost": 1, "goalDifference": 0, "points": 4 } ] },
            { "type": "TOTAL", "group": "Group B", "table": [
              { "position": 1, "team": { "id": 772, "name": "Brazil", "tla": "BRA" }, "playedGames": 3, "won": 2, "draw": 1, "lost": 0, "goalDifference": 5, "points": 7 },
              { "position": 2, "team": { "id": 773, "name": "Germany", "tla": "GER" }, "playedGames": 3, "won": 2, "draw": 0, "lost": 1, "goalDifference": 3, "points": 6 } ] }
          ] }
        """;

    private const string WcMatches =
        """
        { "matches": [
          { "utcDate": "2026-06-30T18:00:00Z", "status": "FINISHED", "stage": "GROUP_STAGE", "group": "GROUP_A",
            "competition": { "name": "FIFA World Cup", "code": "WC" },
            "homeTeam": { "id": 770, "name": "USA", "tla": "USA" },
            "awayTeam": { "id": 771, "name": "Mexico", "tla": "MEX" },
            "score": { "winner": "HOME_TEAM", "duration": "REGULAR", "fullTime": { "home": 2, "away": 1 } } },
          { "utcDate": "2026-07-09T18:00:00Z", "status": "FINISHED", "stage": "QUARTER_FINALS", "group": null,
            "competition": { "name": "FIFA World Cup", "code": "WC" },
            "homeTeam": { "id": 770, "name": "USA", "tla": "USA" },
            "awayTeam": { "id": 772, "name": "Brazil", "tla": "BRA" },
            "score": { "winner": "AWAY_TEAM", "duration": "REGULAR", "fullTime": { "home": 0, "away": 3 } } }
        ] }
        """;

    private static FootballDataClient CreateClient(FootballOptions options)
    {
        var handler = new StubHttpMessageHandler(request =>
            request.RequestUri!.AbsolutePath.Contains("standings", StringComparison.Ordinal)
                ? StubHttpMessageHandler.Json(
                    request.RequestUri.AbsolutePath.Contains("/WC/", StringComparison.Ordinal) ? WcStandings : ClStandings)
                : StubHttpMessageHandler.Json(
                    request.RequestUri.AbsolutePath.Contains("/WC/", StringComparison.Ordinal) ? WcMatches : ClMatches));

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        return new FootballDataClient(
            http, new FakeClock { UtcNow = NowUtc }, Options.Create(options), NullLogger<FootballDataClient>.Instance);
    }

    [Fact]
    public async Task AssemblesChampionsLeague_LeaguePhaseAndBracket()
    {
        var snapshot = await CreateClient(new FootballOptions
        {
            ApiKey = "test-key",
            InterCallDelay = TimeSpan.Zero,
            LeagueCodes = [],
            ChampionsLeagueCode = "CL"
        }).GetFootballAsync();

        Assert.NotNull(snapshot.Cl);
        Assert.Equal(2, snapshot.Cl!.LeaguePhase!.Rows.Count);
        Assert.Equal("UEFA Champions League", snapshot.Cl.LeaguePhase.Name);

        var round = Assert.Single(snapshot.Cl.Bracket!.Rounds);
        Assert.Equal("SEMI_FINALS", round.Stage);
        var tie = Assert.Single(round.Ties);
        Assert.True(tie.TwoLegs);
        Assert.Equal(524, tie.Winner!.Value.Id); // PSG: Aggregat 6:5
    }

    [Fact]
    public async Task AssemblesActiveTournament_GroupsBracketAndWeekCount()
    {
        var snapshot = await CreateClient(new FootballOptions
        {
            ApiKey = "test-key",
            InterCallDelay = TimeSpan.Zero,
            LeagueCodes = [],
            ChampionsLeagueCode = "",
            Tournaments =
            [
                new TournamentConfig
                {
                    Code = "WC",
                    Name = "WM 2026",
                    From = new DateTimeOffset(2026, 6, 11, 0, 0, 0, TimeSpan.Zero),
                    To = new DateTimeOffset(2026, 7, 19, 23, 59, 59, TimeSpan.Zero)
                }
            ]
        }).GetFootballAsync();

        var tournament = Assert.Single(snapshot.Tournaments!);
        Assert.Equal("WM 2026", tournament.Name);
        Assert.Equal(2, tournament.Groups.Count); // Gruppe A + B

        var round = Assert.Single(tournament.Bracket!.Rounds);
        Assert.Equal("QUARTER_FINALS", round.Stage);
        Assert.Equal(772, Assert.Single(round.Ties).Winner!.Value.Id); // Brasilien 3:0

        // Nur das Gruppenspiel am 30.06. liegt in der Woche (Mo 29.06.–So 05.07.); das VF am 09.07. nicht.
        Assert.Equal(1, snapshot.InterestingGamesThisWeek);
    }
}
