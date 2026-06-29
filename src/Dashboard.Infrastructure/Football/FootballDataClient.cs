using System.Globalization;
using System.Net.Http.Json;

using Dashboard.Domain.Football;
using Dashboard.Domain.Time;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dashboard.Infrastructure.Football;

/// <summary>
/// <see cref="IFootballProvider"/> auf Basis von football-data.org v4. Jeder Wettbewerb wird pro Refresh
/// <b>genau einmal</b> abgefragt – Spiele (<c>competitions/{code}/matches</c>) und Tabelle
/// (<c>competitions/{code}/standings</c>) –, danach werden alle getrackten Vereine client-seitig daraus
/// aufgelöst. Das spart Calls (12 Vereine über 5 Ligen + CL ⇒ ~11 statt ~24) und schont das Free-Tier-
/// Limit (10/min), zusätzlich abgesichert durch <see cref="FootballOptions.InterCallDelay"/>.
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
        var now = _clock.UtcNow;
        // Datumsfenster, damit auch über die Saisongrenze hinweg jüngste Ergebnisse erscheinen.
        var from = now.AddDays(-90).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = now.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Drosselung über alle Calls eines Refreshs hinweg (durchgehendes Rate-Budget).
        var firstCall = true;
        async Task ThrottleAsync()
        {
            if (!firstCall && _options.InterCallDelay > TimeSpan.Zero)
            {
                await Task.Delay(_options.InterCallDelay, ct);
            }

            firstCall = false;
        }

        // 1) Spiele je Wettbewerb genau einmal (Union der Wettbewerbe aller getrackten Vereine).
        var matchesByComp = new Dictionary<string, IReadOnlyList<FdMatch>>(StringComparer.OrdinalIgnoreCase);
        foreach (var comp in _options.Teams.SelectMany(CompetitionsOf).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await ThrottleAsync();
                var response = await GetAsync<FdMatchesResponse>(
                    $"v4/competitions/{comp}/matches?dateFrom={from}&dateTo={to}", ct);
                matchesByComp[comp] = response.Matches;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fußball: Spiele für Wettbewerb {Comp} übersprungen.", comp);
            }
        }

        // 2) Tabellen je Liga genau einmal: konfigurierte Top-5-Ligen ∪ Liga jedes Vereins.
        var standingsByComp = new Dictionary<string, FdStandingsResponse>(StringComparer.OrdinalIgnoreCase);
        var standingsCodes = _options.LeagueCodes
            .Concat(_options.Teams.Select(t => t.CompetitionCode))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var code in standingsCodes)
        {
            try
            {
                await ThrottleAsync();
                standingsByComp[code] = await GetAsync<FdStandingsResponse>(
                    $"v4/competitions/{code}/standings", ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fußball: Tabelle {Comp} übersprungen.", code);
            }
        }

        var trackedIds = _options.Teams.Select(t => t.TeamId).ToHashSet();

        // 3) Top-5-Ligatabellen in konfigurierter Reihenfolge; jeder hier getrackte Verein wird markiert.
        var leagueTables = new List<LeagueTable>();
        foreach (var code in _options.LeagueCodes)
        {
            if (standingsByComp.TryGetValue(code, out var response)
                && ExtractRows(response, trackedIds) is { Count: > 0 } rows)
            {
                leagueTables.Add(new LeagueTable(code, response.Competition?.Name ?? code, rows));
            }
        }

        // 4) Vereins-Sichten + Wochen-Fixtures aus den gecachten Daten ableiten (keine weiteren Calls).
        var teams = new List<FootballTeamSnapshot>(_options.Teams.Count);
        var fixtures = new List<FixtureKey>();
        foreach (var team in _options.Teams)
        {
            var teamMatches = CompetitionsOf(team)
                .Where(matchesByComp.ContainsKey)
                .SelectMany(comp => matchesByComp[comp])
                .Where(m => m.HomeTeam.Id == team.TeamId || m.AwayTeam.Id == team.TeamId)
                .ToList();

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

            IReadOnlyList<LeagueRow> table = standingsByComp.TryGetValue(team.CompetitionCode, out var standings)
                ? ExtractRows(standings, [team.TeamId])
                : [];
            var standing = table.FirstOrDefault(r => r.IsOwnTeam) is { } own
                ? new TablePosition(own.Position, own.PlayedGames, own.Points)
                : null;

            teams.Add(new FootballTeamSnapshot(team.Name, recent, upcoming, standing, table));

            fixtures.AddRange(teamMatches.Select(m => new FixtureKey(
                m.Competition.Code ?? m.Competition.Name, m.UtcDate, m.HomeTeam.Id, m.AwayTeam.Id)));
        }

        var interestingGames = FootballWeekCounter.CountInterestingGames(fixtures, now);

        return new FootballSnapshot(teams, now, leagueTables, interestingGames);
    }

    private static IEnumerable<string> CompetitionsOf(FootballTeamConfig team)
    {
        IEnumerable<string> comps = team.Competitions.Count > 0 ? team.Competitions : [team.CompetitionCode];
        return comps.Where(c => !string.IsNullOrWhiteSpace(c));
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

    private static IReadOnlyList<LeagueRow> ExtractRows(FdStandingsResponse standings, IReadOnlyCollection<int> ownTeamIds)
    {
        var total = standings.Standings.FirstOrDefault(s => s.Type == "TOTAL");
        if (total is null)
        {
            return [];
        }

        return total.Table
            .Select(e => new LeagueRow(
                e.Position,
                OpponentName(e.Team),
                e.Team.Tla,
                e.PlayedGames,
                e.Won,
                e.Draw,
                e.Lost,
                e.GoalDifference,
                e.Points,
                IsOwnTeam: ownTeamIds.Contains(e.Team.Id)))
            .ToList();
    }
}
