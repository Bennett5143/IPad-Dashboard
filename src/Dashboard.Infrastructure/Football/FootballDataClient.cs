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
/// (<c>competitions/{code}/standings</c>) –, danach werden Vereine, Top-5-Tabellen, Champions League
/// (Ligaphase + K.o.-Bracket) und aktive Turniere (EM/WM: Gruppen + Bracket) client-seitig daraus
/// abgeleitet. Ligen nutzen ein enges Datumsfenster; CL/Turniere die volle Saison (für den Bracket).
/// <see cref="FootballOptions.InterCallDelay"/> drosselt unter das Free-Tier-Limit (10/min).
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
        // Enges Fenster für Liga-Spiele (jüngste Ergebnisse über die Saisongrenze hinweg).
        var narrowFrom = now.AddDays(-90).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var narrowTo = now.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Wettbewerbe, die die volle Saison brauchen (für Gruppen/Bracket): CL + aktive Turniere.
        var fullSeasonCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(_options.ChampionsLeagueCode))
        {
            fullSeasonCodes.Add(_options.ChampionsLeagueCode);
        }

        var activeTournaments = _options.Tournaments
            .Where(t => !string.IsNullOrWhiteSpace(t.Code) && t.From <= now && now <= t.To)
            .ToList();
        foreach (var tournament in activeTournaments)
        {
            fullSeasonCodes.Add(tournament.Code);
        }

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

        // 1) Spiele je Wettbewerb genau einmal (Team-Wettbewerbe ∪ Voll-Saison-Codes).
        var matchesByComp = new Dictionary<string, IReadOnlyList<FdMatch>>(StringComparer.OrdinalIgnoreCase);
        var matchComps = _options.Teams.SelectMany(CompetitionsOf)
            .Concat(fullSeasonCodes)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var comp in matchComps)
        {
            try
            {
                await ThrottleAsync();
                var url = fullSeasonCodes.Contains(comp)
                    ? $"v4/competitions/{comp}/matches"
                    : $"v4/competitions/{comp}/matches?dateFrom={narrowFrom}&dateTo={narrowTo}";
                var response = await GetAsync<FdMatchesResponse>(url, ct);
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

        // 2) Tabellen je Liga genau einmal: Top-5-Ligen ∪ Liga jedes Vereins ∪ Voll-Saison-Codes.
        var standingsByComp = new Dictionary<string, FdStandingsResponse>(StringComparer.OrdinalIgnoreCase);
        var standingsCodes = _options.LeagueCodes
            .Concat(_options.Teams.Select(t => t.CompetitionCode))
            .Concat(fullSeasonCodes)
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
        var weekFixtures = new List<FixtureKey>();
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
            weekFixtures.AddRange(teamMatches.Select(ToFixtureKey));
        }

        // 5) Champions League: Ligaphase-Tabelle + K.o.-Bracket.
        var cl = BuildChampionsLeague(standingsByComp, matchesByComp, trackedIds);

        // 6) Aktive Turniere: Gruppentabellen + K.o.-Bracket; ihre Spiele zählen für die Wochensicht mit.
        var tournaments = new List<TournamentView>();
        foreach (var tournament in activeTournaments)
        {
            var view = BuildTournament(tournament, standingsByComp, matchesByComp, trackedIds);
            if (view is not null)
            {
                tournaments.Add(view);
            }

            if (matchesByComp.TryGetValue(tournament.Code, out var tMatches))
            {
                weekFixtures.AddRange(tMatches
                    .Where(m => m.HomeTeam.Id is not null && m.AwayTeam.Id is not null)
                    .Select(ToFixtureKey));
            }
        }

        var interestingGames = FootballWeekCounter.CountInterestingGames(weekFixtures, now);

        return new FootballSnapshot(
            teams, now, leagueTables, interestingGames,
            cl, tournaments.Count > 0 ? tournaments : null);
    }

    private ChampionsLeague? BuildChampionsLeague(
        IReadOnlyDictionary<string, FdStandingsResponse> standingsByComp,
        IReadOnlyDictionary<string, IReadOnlyList<FdMatch>> matchesByComp,
        IReadOnlyCollection<int> trackedIds)
    {
        var code = _options.ChampionsLeagueCode;
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        LeagueTable? leaguePhase = null;
        if (standingsByComp.TryGetValue(code, out var standings)
            && ExtractRows(standings, trackedIds) is { Count: > 0 } rows)
        {
            leaguePhase = new LeagueTable(code, standings.Competition?.Name ?? code, rows);
        }

        var bracket = BuildBracket(code, matchesByComp);

        return leaguePhase is null && bracket is null
            ? null
            : new ChampionsLeague(leaguePhase, bracket);
    }

    private static TournamentView? BuildTournament(
        TournamentConfig tournament,
        IReadOnlyDictionary<string, FdStandingsResponse> standingsByComp,
        IReadOnlyDictionary<string, IReadOnlyList<FdMatch>> matchesByComp,
        IReadOnlyCollection<int> trackedIds)
    {
        var groups = standingsByComp.TryGetValue(tournament.Code, out var standings)
            ? ExtractGroupTables(standings, trackedIds)
            : [];
        var bracket = BuildBracket(tournament.Code, matchesByComp);

        return groups.Count == 0 && bracket is null
            ? null
            : new TournamentView(tournament.Code, tournament.Name, groups, bracket);
    }

    private static KnockoutBracket? BuildBracket(
        string code, IReadOnlyDictionary<string, IReadOnlyList<FdMatch>> matchesByComp)
    {
        if (!matchesByComp.TryGetValue(code, out var matches))
        {
            return null;
        }

        var bracket = KnockoutBracketBuilder.Build(matches.Select(ToFixture));
        return bracket.Rounds.Count > 0 ? bracket : null;
    }

    private static IEnumerable<string> CompetitionsOf(FootballTeamConfig team)
    {
        IEnumerable<string> comps = team.Competitions.Count > 0 ? team.Competitions : [team.CompetitionCode];
        return comps.Where(c => !string.IsNullOrWhiteSpace(c));
    }

    private static FixtureKey ToFixtureKey(FdMatch m) => new(
        m.Competition.Code ?? m.Competition.Name, m.UtcDate, m.HomeTeam.Id ?? 0, m.AwayTeam.Id ?? 0);

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
        var ownGoals = isHome ? match.Score.FullTime?.Home : match.Score.FullTime?.Away;
        var opponentGoals = isHome ? match.Score.FullTime?.Away : match.Score.FullTime?.Home;

        return new Match(
            match.UtcDate, match.Competition.Code ?? match.Competition.Name, opponent, isHome, ownGoals, opponentGoals);
    }

    private static Fixture ToFixture(FdMatch match)
    {
        var status = match.Status switch
        {
            "IN_PLAY" or "PAUSED" => MatchStatus.Live,
            "FINISHED" => MatchStatus.Finished,
            _ => MatchStatus.Scheduled
        };

        var (homeGoals, awayGoals, homePenalties, awayPenalties, afterExtraTime) = ResolveScore(match.Score);

        return new Fixture(
            match.UtcDate,
            match.Stage ?? string.Empty,
            match.Group,
            ToTeamRef(match.HomeTeam),
            ToTeamRef(match.AwayTeam),
            homeGoals, awayGoals, homePenalties, awayPenalties, afterExtraTime, status);
    }

    private static TeamRef ToTeamRef(FdTeam team) =>
        new(team.Id ?? 0, OpponentName(team), team.Tla);

    // On-Pitch-Tore (regulär + Verlängerung) von einem etwaigen Elfmeterschießen trennen, da
    // fullTime bei Schießen die Elfmeter MIT enthält.
    private static (int?, int?, int?, int?, bool) ResolveScore(FdScore? score)
    {
        if (score is null)
        {
            return (null, null, null, null, false);
        }

        var shootout = string.Equals(score.Duration, "PENALTY_SHOOTOUT", StringComparison.Ordinal);
        var afterExtraTime = shootout || string.Equals(score.Duration, "EXTRA_TIME", StringComparison.Ordinal);

        if (shootout)
        {
            var homeOnPitch = (score.RegularTime?.Home ?? 0) + (score.ExtraTime?.Home ?? 0);
            var awayOnPitch = (score.RegularTime?.Away ?? 0) + (score.ExtraTime?.Away ?? 0);
            return (homeOnPitch, awayOnPitch, score.Penalties?.Home, score.Penalties?.Away, afterExtraTime);
        }

        return (score.FullTime?.Home, score.FullTime?.Away, null, null, afterExtraTime);
    }

    // Voller Vereinsname statt shortName: letzterer ist teils zu knapp/mehrdeutig
    // (z. B. "Athletic" für Athletic Club Bilbao). shortName nur als Fallback.
    private static string OpponentName(FdTeam team) =>
        !string.IsNullOrWhiteSpace(team.Name) ? team.Name : team.ShortName ?? string.Empty;

    private static IReadOnlyList<LeagueRow> ExtractRows(FdStandingsResponse standings, IReadOnlyCollection<int> ownTeamIds)
    {
        var total = standings.Standings.FirstOrDefault(s => s.Type == "TOTAL");
        return total is null ? [] : total.Table.Select(e => ToRow(e, ownTeamIds)).ToList();
    }

    private static IReadOnlyList<LeagueTable> ExtractGroupTables(
        FdStandingsResponse standings, IReadOnlyCollection<int> ownTeamIds) =>
        standings.Standings
            .Where(s => s.Type == "TOTAL")
            .Select(s => new LeagueTable(
                s.Group ?? string.Empty,
                s.Group ?? string.Empty,
                s.Table.Select(e => ToRow(e, ownTeamIds)).ToList()))
            .ToList();

    private static LeagueRow ToRow(FdTableEntry entry, IReadOnlyCollection<int> ownTeamIds) => new(
        entry.Position,
        OpponentName(entry.Team),
        entry.Team.Tla,
        entry.PlayedGames,
        entry.Won,
        entry.Draw,
        entry.Lost,
        entry.GoalDifference,
        entry.Points,
        IsOwnTeam: entry.Team.Id is int id && ownTeamIds.Contains(id));
}
