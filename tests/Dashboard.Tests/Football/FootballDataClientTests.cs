using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dashboard.Tests.Football;

public class FootballDataClientTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);

    private const string MatchesJson =
        """
        {
          "matches": [
            { "utcDate": "2026-05-20T15:00:00Z", "status": "FINISHED",
              "competition": { "name": "La Liga", "code": "PD" },
              "homeTeam": { "id": 86, "name": "Real Madrid CF", "shortName": "Real Madrid", "tla": "RMA" },
              "awayTeam": { "id": 81, "name": "FC Barcelona", "shortName": "Barça", "tla": "FCB" },
              "score": { "fullTime": { "home": 2, "away": 1 } } },
            { "utcDate": "2026-05-24T15:00:00Z", "status": "FINISHED",
              "competition": { "name": "La Liga", "code": "PD" },
              "homeTeam": { "id": 90, "name": "Real Betis", "shortName": "Betis", "tla": "BET" },
              "awayTeam": { "id": 86, "name": "Real Madrid CF", "shortName": "Real Madrid", "tla": "RMA" },
              "score": { "fullTime": { "home": 3, "away": 1 } } },
            { "utcDate": "2026-06-01T19:00:00Z", "status": "TIMED",
              "competition": { "name": "La Liga", "code": "PD" },
              "homeTeam": { "id": 86, "name": "Real Madrid CF", "shortName": "Real Madrid", "tla": "RMA" },
              "awayTeam": { "id": 95, "name": "Valencia CF", "shortName": "Valencia", "tla": "VAL" },
              "score": { "fullTime": { "home": null, "away": null } } },
            { "utcDate": "2026-06-08T19:00:00Z", "status": "SCHEDULED",
              "competition": { "name": "La Liga", "code": "PD" },
              "homeTeam": { "id": 50, "name": "Sevilla FC", "shortName": "Sevilla", "tla": "SEV" },
              "awayTeam": { "id": 86, "name": "Real Madrid CF", "shortName": "Real Madrid", "tla": "RMA" },
              "score": { "fullTime": { "home": null, "away": null } } }
          ]
        }
        """;

    private const string StandingsJson =
        """
        {
          "standings": [
            { "type": "HOME", "table": [ { "position": 5, "team": { "id": 86, "name": "Real Madrid CF" }, "playedGames": 18, "points": 40 } ] },
            { "type": "TOTAL", "table": [
              { "position": 2, "team": { "id": 81, "name": "FC Barcelona" }, "playedGames": 36, "points": 80 },
              { "position": 1, "team": { "id": 86, "name": "Real Madrid CF" }, "playedGames": 36, "points": 85 }
            ] }
          ]
        }
        """;

    private static FootballDataClient CreateClient(int recent = 2, int upcoming = 2)
    {
        var handler = new StubHttpMessageHandler(request =>
            request.RequestUri!.AbsolutePath.Contains("standings", StringComparison.Ordinal)
                ? StubHttpMessageHandler.Json(StandingsJson)
                : StubHttpMessageHandler.Json(MatchesJson));

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var options = Options.Create(new FootballOptions
        {
            ApiKey = "test-key",
            RecentCount = recent,
            UpcomingCount = upcoming,
            Teams = [new FootballTeamConfig { Name = "Real Madrid", TeamId = 86, CompetitionCode = "PD" }]
        });

        return new FootballDataClient(
            http, new FakeClock { UtcNow = NowUtc }, options, NullLogger<FootballDataClient>.Instance);
    }

    [Fact]
    public async Task GetFootballAsync_ReturnsConfiguredTeam_WithRetrievalTimestamp()
    {
        var snapshot = await CreateClient().GetFootballAsync();

        Assert.Single(snapshot.Teams);
        Assert.Equal("Real Madrid", snapshot.Teams[0].TeamName);
        Assert.Equal(NowUtc, snapshot.RetrievedAtUtc);
    }

    [Fact]
    public async Task GetFootballAsync_OrdersRecentResultsNewestFirst_AndResolvesPerspective()
    {
        var team = (await CreateClient().GetFootballAsync()).Teams[0];

        Assert.Equal(2, team.RecentResults.Count);

        // Neuestes zuerst: Auswärts bei Betis 1:3 (aus Real-Sicht) → Niederlage
        var latest = team.RecentResults[0];
        Assert.Equal("Betis", latest.Opponent);
        Assert.False(latest.IsHome);
        Assert.Equal(1, latest.OwnGoals);
        Assert.Equal(3, latest.OpponentGoals);
        Assert.Equal(MatchOutcome.Loss, latest.Outcome);

        // Davor: Heim gegen Barça 2:1 → Sieg
        var earlier = team.RecentResults[1];
        Assert.Equal("Barça", earlier.Opponent);
        Assert.True(earlier.IsHome);
        Assert.Equal(MatchOutcome.Win, earlier.Outcome);
    }

    [Fact]
    public async Task GetFootballAsync_OrdersUpcomingSoonestFirst_AndMarksUnfinished()
    {
        var team = (await CreateClient().GetFootballAsync()).Teams[0];

        Assert.Equal(2, team.Upcoming.Count);

        var next = team.Upcoming[0];
        Assert.Equal("Valencia", next.Opponent);
        Assert.True(next.IsHome);
        Assert.False(next.IsFinished);
        Assert.Null(next.Outcome);

        Assert.Equal("Sevilla", team.Upcoming[1].Opponent);
        Assert.False(team.Upcoming[1].IsHome);
    }

    [Fact]
    public async Task GetFootballAsync_ExtractsTotalStandingForTeam()
    {
        var team = (await CreateClient().GetFootballAsync()).Teams[0];

        Assert.NotNull(team.Standing);
        Assert.Equal(1, team.Standing!.Position);      // aus TOTAL, nicht HOME
        Assert.Equal(36, team.Standing.PlayedGames);
        Assert.Equal(85, team.Standing.Points);
    }

    [Fact]
    public async Task GetFootballAsync_RespectsCounts()
    {
        var team = (await CreateClient(recent: 1, upcoming: 1).GetFootballAsync()).Teams[0];

        Assert.Single(team.RecentResults);
        Assert.Single(team.Upcoming);
    }
}
