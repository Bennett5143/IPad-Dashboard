using System.Net.Http.Json;

using Dashboard.Domain.Football;
using Dashboard.Domain.Time;

using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Football;

/// <summary>
/// <see cref="IFootballProvider"/> auf Basis von football-data.org v4. Pro Verein ein Aufruf der
/// Saison-Spielliste (<c>teams/{id}/matches</c>) und der Ligatabelle (<c>competitions/{code}/standings</c>).
/// Ergebnisse/Spiele werden in die Vereins-Perspektive aufgelöst (Gegner, Heim/Auswärts, eigene Tore).
/// </summary>
public sealed class FootballDataClient : IFootballProvider
{
    private static readonly string[] UpcomingStatuses = ["SCHEDULED", "TIMED"];

    private readonly HttpClient _http;
    private readonly IClock _clock;
    private readonly FootballOptions _options;

    public FootballDataClient(HttpClient http, IClock clock, IOptions<FootballOptions> options)
    {
        _http = http;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<FootballSnapshot> GetFootballAsync(CancellationToken ct = default)
    {
        var teams = new List<FootballTeamSnapshot>(_options.Teams.Count);

        // Bewusst sequenziell – schont das Rate-Limit des Free-Tiers (10 Requests/min).
        foreach (var team in _options.Teams)
        {
            teams.Add(await GetTeamAsync(team, ct));
        }

        return new FootballSnapshot(teams, _clock.UtcNow);
    }

    private async Task<FootballTeamSnapshot> GetTeamAsync(FootballTeamConfig team, CancellationToken ct)
    {
        var matches = await _http.GetFromJsonAsync<FdMatchesResponse>(
            $"v4/teams/{team.TeamId}/matches", ct)
            ?? throw new InvalidOperationException($"Leere Antwort (matches) für Team {team.TeamId}.");

        var standings = await _http.GetFromJsonAsync<FdStandingsResponse>(
            $"v4/competitions/{team.CompetitionCode}/standings", ct)
            ?? throw new InvalidOperationException($"Leere Antwort (standings) für {team.CompetitionCode}.");

        var recent = matches.Matches
            .Where(m => m.Status == "FINISHED")
            .OrderByDescending(m => m.UtcDate)
            .Take(_options.RecentCount)
            .Select(m => MapMatch(m, team.TeamId))
            .ToList();

        var upcoming = matches.Matches
            .Where(m => UpcomingStatuses.Contains(m.Status))
            .OrderBy(m => m.UtcDate)
            .Take(_options.UpcomingCount)
            .Select(m => MapMatch(m, team.TeamId))
            .ToList();

        return new FootballTeamSnapshot(team.Name, recent, upcoming, ExtractStanding(standings, team.TeamId));
    }

    private static Match MapMatch(FdMatch match, int teamId)
    {
        var isHome = match.HomeTeam.Id == teamId;
        var opponent = OpponentName(isHome ? match.AwayTeam : match.HomeTeam);
        var ownGoals = isHome ? match.Score.FullTime.Home : match.Score.FullTime.Away;
        var opponentGoals = isHome ? match.Score.FullTime.Away : match.Score.FullTime.Home;

        return new Match(match.UtcDate, match.Competition.Name, opponent, isHome, ownGoals, opponentGoals);
    }

    private static string OpponentName(FdTeam team) =>
        !string.IsNullOrWhiteSpace(team.ShortName) ? team.ShortName : team.Name;

    private static TablePosition? ExtractStanding(FdStandingsResponse standings, int teamId)
    {
        var total = standings.Standings.FirstOrDefault(s => s.Type == "TOTAL");
        var row = total?.Table.FirstOrDefault(t => t.Team.Id == teamId);

        return row is null ? null : new TablePosition(row.Position, row.PlayedGames, row.Points);
    }
}
