using System.Globalization;

namespace Dashboard.Web.Components.Tiles;

/// <summary>
/// Ein Eintrag in einer Tagesspalte des Wochenkalenders: entweder die Begegnung eines verfolgten
/// Vereins oder — mit gesetztem <paramref name="AggregateLabel"/> — ein zusammengefasster
/// Champions-League-Spieltag.
/// </summary>
public sealed record FootballWeekEntry(
    DateTimeOffset KickoffUtc,
    string Time,
    string TeamName,
    string Opponent,
    string Venue,
    string CompetitionCode,
    string? Result,
    bool IsFinished,
    string? AggregateLabel = null)
{
    /// <summary>Ein Champions-League-Spieltag, der mehrere Begegnungen zusammenfasst.</summary>
    public bool IsAggregate => AggregateLabel is not null;

    /// <summary>Was in der Tagesspalte steht.</summary>
    public string Title => AggregateLabel ?? $"{TeamName} – {Opponent}";
}

/// <summary>Ein Tag der aktuellen Woche mit seinen Einträgen.</summary>
public sealed record FootballWeekDay(
    DateOnly Date,
    string DayLabel,
    string DateLabel,
    string Label,
    bool IsToday,
    bool IsPast,
    IReadOnlyList<FootballWeekEntry> Entries);

/// <summary>
/// Baut die Wochenansicht Mo–So (Berlin) aus den Spielen aller verfolgten Vereine — reine, testbare
/// Aufbereitung. „Heute" stammt aus dem Snapshot-Zeitstempel.
/// <para>
/// Champions-League-Spiele eines Tages werden zu <em>einem</em> Eintrag verdichtet: an einem
/// CL-Spieltag interessiert der Spieltag, nicht sechs einzelne Zeilen. Die beteiligten Vereine
/// erscheinen dann nicht zusätzlich einzeln.
/// </para>
/// </summary>
public static class FootballWeekBuilder
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    /// <summary>Stages, in denen der Spieltag die Runde benennt (statt nur das Hin-/Rückspiel).</summary>
    private static readonly string[] MatchdayStages = ["LEAGUE_STAGE", "GROUP_STAGE", "REGULAR_SEASON"];

    public static IReadOnlyList<FootballWeekDay> Build(
        FootballSnapshot snapshot, string championsLeagueCode = "CL")
    {
        var today = BerlinDate(snapshot.RetrievedAtUtc);
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); // Mo-basiert
        var sunday = monday.AddDays(6);

        var week = snapshot.Teams
            .SelectMany(team => team.RecentResults.Concat(team.Upcoming)
                .Select(match => (Team: team.TeamName, Match: match, Date: BerlinDate(match.KickoffUtc))))
            .Where(x => x.Date >= monday && x.Date <= sunday)
            .ToLookup(x => x.Date);

        return Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = monday.AddDays(offset);
                var day = date.ToDateTime(TimeOnly.MinValue);

                return new FootballWeekDay(
                    date,
                    day.ToString("ddd", German).ToUpperInvariant(),
                    day.ToString("dd.MM.", German),
                    day.ToString("ddd dd.MM.", German),
                    date == today,
                    date < today,
                    Entries(week[date], championsLeagueCode));
            })
            .ToList();
    }

    /// <summary>Gibt es überhaupt Spiele in der Woche?</summary>
    public static bool HasMatches(IReadOnlyList<FootballWeekDay> week) => week.Any(d => d.Entries.Count > 0);

    private static IReadOnlyList<FootballWeekEntry> Entries(
        IEnumerable<(string Team, Match Match, DateOnly Date)> ofDay, string championsLeagueCode)
    {
        var all = ofDay.ToList();

        var entries = all
            .Where(x => !IsChampionsLeague(x.Match, championsLeagueCode))
            .Select(x => ClubEntry(x.Team, x.Match))
            .ToList();

        var clMatches = all
            .Where(x => IsChampionsLeague(x.Match, championsLeagueCode))
            .Select(x => x.Match)
            .ToList();

        if (clMatches.Count > 0)
        {
            entries.Add(AggregateEntry(clMatches, championsLeagueCode));
        }

        return entries.OrderBy(entry => entry.KickoffUtc).ToList();
    }

    private static bool IsChampionsLeague(Match match, string championsLeagueCode) =>
        !string.IsNullOrWhiteSpace(championsLeagueCode)
        && string.Equals(match.CompetitionCode, championsLeagueCode, StringComparison.OrdinalIgnoreCase);

    private static FootballWeekEntry ClubEntry(string team, Match match) => new(
        match.KickoffUtc,
        Time(match.KickoffUtc),
        team,
        match.Opponent,
        match.IsHome ? "H" : "A",
        match.CompetitionCode,
        match.IsFinished ? $"{match.OwnGoals}:{match.OpponentGoals}" : null,
        match.IsFinished);

    /// <summary>
    /// Ein Eintrag für alle CL-Spiele des Tages, benannt nach Spieltag bzw. K.o.-Runde. Die Uhrzeit
    /// ist der früheste Anstoß des Tages; ein Ergebnis trägt der Sammeleintrag bewusst nicht.
    /// </summary>
    private static FootballWeekEntry AggregateEntry(
        IReadOnlyList<Match> clMatches, string championsLeagueCode)
    {
        var earliest = clMatches.Min(match => match.KickoffUtc);

        return new FootballWeekEntry(
            earliest,
            Time(earliest),
            TeamName: string.Empty,
            Opponent: string.Empty,
            Venue: string.Empty,
            championsLeagueCode,
            Result: null,
            IsFinished: clMatches.All(match => match.IsFinished),
            AggregateLabel: AggregateLabel(clMatches));
    }

    // Die Ligaphase benennt sich über den Spieltag, die K.o.-Phase über die Runde: dort zählt
    // matchday nur das Hin-/Rückspiel und wäre als „Spieltag 1" schlicht falsch.
    private static string AggregateLabel(IReadOnlyList<Match> clMatches)
    {
        const string Competition = "Champions League";

        var stage = clMatches
            .Select(match => match.Stage)
            .FirstOrDefault(stage => !string.IsNullOrWhiteSpace(stage));

        if (stage is not null && !MatchdayStages.Contains(stage, StringComparer.OrdinalIgnoreCase))
        {
            return $"{Competition} · {KnockoutBracketBuilder.StageLabel(stage)}";
        }

        return clMatches.Select(match => match.Matchday).FirstOrDefault(day => day is not null) is { } matchday
            ? $"{Competition} · Spieltag {matchday}"
            : Competition;
    }

    private static string Time(DateTimeOffset kickoffUtc) =>
        TimeZoneInfo.ConvertTime(kickoffUtc, BerlinTz).ToString("HH:mm", German);

    private static DateOnly BerlinDate(DateTimeOffset utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, BerlinTz).DateTime);
}
