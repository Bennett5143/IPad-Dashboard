using System.Globalization;
using System.Net.Http.Json;

using Dashboard.Domain.Football;
using Dashboard.Domain.Time;

using Microsoft.Extensions.Logging;
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
    private readonly ILogger<FootballDataClient> _logger;

    public FootballDataClient(
        HttpClient http, IClock clock, IOptions<FootballOptions> options, ILogger<FootballDataClient> logger)
    {
        _http = http;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FootballSnapshot> GetFootballAsync(CancellationToken ct = default)
    {
        var teams = new List<FootballTeamSnapshot>(_options.Teams.Count);

        // Bewusst sequenziell – schont das Rate-Limit des Free-Tiers (10 Requests/min).
        // Ein einzelner fehlschlagender Verein (z. B. falsche Id, 403) darf die anderen nicht mitreißen.
        foreach (var team in _options.Teams)
        {
            try
            {
                teams.Add(await GetTeamAsync(team, ct));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fußball: Verein {Team} (Id {Id}) konnte nicht geladen werden – übersprungen.",
                    team.Name, team.TeamId);
            }
        }

        return new FootballSnapshot(teams, _clock.UtcNow);
    }

    private async Task<FootballTeamSnapshot> GetTeamAsync(FootballTeamConfig team, CancellationToken ct)
    {
        // Wettbewerbs-Endpoint statt teams/{id}/matches: letzterer 403t im Free-Tier, wenn der
        // Verein auch in nicht-freien Wettbewerben (Pokal etc.) spielt – die Liga selbst ist frei.
        // Datumsfenster, damit auch über die Saisongrenze hinweg jüngste Ergebnisse erscheinen.
        var now = _clock.UtcNow;
        var from = now.AddDays(-90).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = now.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        IReadOnlyList<string> competitions =
            team.Competitions.Count > 0 ? team.Competitions : [team.CompetitionCode];

        // Spiele aus allen konfigurierten Wettbewerben (Liga + z. B. CL) sammeln. Ein einzelner
        // nicht erreichbarer Wettbewerb darf den Rest nicht blockieren.
        var teamMatches = new List<FdMatch>();
        foreach (var comp in competitions)
        {
            try
            {
                var response = await GetAsync<FdMatchesResponse>(
                    $"v4/competitions/{comp}/matches?dateFrom={from}&dateTo={to}", ct);
                teamMatches.AddRange(response.Matches
                    .Where(m => m.HomeTeam.Id == team.TeamId || m.AwayTeam.Id == team.TeamId));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fußball: Wettbewerb {Comp} für {Team} übersprungen.", comp, team.Name);
            }
        }

        var recent = teamMatches
            .Where(m => m.Status == "FINISHED")
            .OrderByDescending(m => m.UtcDate)
            .Take(_options.RecentCount)
            .Select(m => MapMatch(m, team.TeamId))
            .ToList();

        var upcoming = teamMatches
            .Where(m => UpcomingStatuses.Contains(m.Status))
            .OrderBy(m => m.UtcDate)
            .Take(_options.UpcomingCount)
            .Select(m => MapMatch(m, team.TeamId))
            .ToList();

        return new FootballTeamSnapshot(team.Name, recent, upcoming, await GetStandingAsync(team, ct));
    }

    private async Task<TablePosition?> GetStandingAsync(FootballTeamConfig team, CancellationToken ct)
    {
        try
        {
            var standings = await GetAsync<FdStandingsResponse>(
                $"v4/competitions/{team.CompetitionCode}/standings", ct);
            return ExtractStanding(standings, team.TeamId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fußball: Tabelle {Comp} für {Team} übersprungen.", team.CompetitionCode, team.Name);
            return null;
        }
    }

    // Liest die Antwort und macht den echten football-data.org-Fehler (Status + Body) sichtbar –
    // z. B. 403 "check your subscription", 404 (Team/Wettbewerb), 429 (Rate-Limit).
    private async Task<T> GetAsync<T>(string url, CancellationToken ct)
    {
        using var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"football-data.org {(int)response.StatusCode} bei '{url}': {body}");
        }

        return await response.Content.ReadFromJsonAsync<T>(ct)
            ?? throw new InvalidOperationException($"Leere Antwort von '{url}'.");
    }

    private static Match MapMatch(FdMatch match, int teamId)
    {
        var isHome = match.HomeTeam.Id == teamId;
        var opponent = OpponentName(isHome ? match.AwayTeam : match.HomeTeam);
        var ownGoals = isHome ? match.Score.FullTime.Home : match.Score.FullTime.Away;
        var opponentGoals = isHome ? match.Score.FullTime.Away : match.Score.FullTime.Home;

        return new Match(
            match.UtcDate, match.Competition.Code ?? match.Competition.Name, opponent, isHome, ownGoals, opponentGoals);
    }

    // Voller Vereinsname statt shortName: letzterer ist teils zu knapp/mehrdeutig
    // (z. B. "Athletic" für Athletic Club Bilbao). shortName nur als Fallback.
    private static string OpponentName(FdTeam team) =>
        !string.IsNullOrWhiteSpace(team.Name) ? team.Name : team.ShortName ?? string.Empty;

    private static TablePosition? ExtractStanding(FdStandingsResponse standings, int teamId)
    {
        var total = standings.Standings.FirstOrDefault(s => s.Type == "TOTAL");
        var row = total?.Table.FirstOrDefault(t => t.Team.Id == teamId);

        return row is null ? null : new TablePosition(row.Position, row.PlayedGames, row.Points);
    }
}
