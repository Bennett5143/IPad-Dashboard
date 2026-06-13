using System.Globalization;

namespace Dashboard.Web.Components.Tiles;

/// <summary>Ein Spiel in der Wochen-Kalenderansicht (FA-4.05).</summary>
public sealed record FootballWeekEntry(
    string Time, string TeamName, string Opponent, string Venue, string CompetitionCode, string? Result, bool IsFinished);

/// <summary>Ein Tag der aktuellen Woche mit seinen Spielen.</summary>
public sealed record FootballWeekDay(string Label, bool IsToday, IReadOnlyList<FootballWeekEntry> Entries);

/// <summary>
/// Baut die Kalenderansicht der aktuellen Woche (Mo–So, Berlin) aus den Spielen aller Vereine
/// (FA-4.05) — reine, testbare Aufbereitung. „Heute" stammt aus dem Snapshot-Zeitstempel.
/// </summary>
public static class FootballWeekBuilder
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static IReadOnlyList<FootballWeekDay> Build(FootballSnapshot snapshot)
    {
        var today = BerlinDate(snapshot.RetrievedAtUtc);
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); // Mo-basiert

        // Beendete (Recent) und kommende Spiele aller Vereine einsammeln und der Woche zuordnen.
        var byDate = snapshot.Teams
            .SelectMany(team => team.RecentResults.Concat(team.Upcoming)
                .Select(match => (Team: team.TeamName, Match: match, Date: BerlinDate(match.KickoffUtc))))
            .Where(x => x.Date >= monday && x.Date <= monday.AddDays(6))
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g
                .OrderBy(x => x.Match.KickoffUtc)
                .Select(x => new FootballWeekEntry(
                    TimeZoneInfo.ConvertTime(x.Match.KickoffUtc, BerlinTz).ToString("HH:mm", German),
                    x.Team,
                    x.Match.Opponent,
                    x.Match.IsHome ? "H" : "A",
                    x.Match.CompetitionCode,
                    x.Match.IsFinished ? $"{x.Match.OwnGoals}:{x.Match.OpponentGoals}" : null,
                    x.Match.IsFinished))
                .ToList());

        return Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = monday.AddDays(offset);
                return new FootballWeekDay(
                    date.ToString("ddd dd.MM.", German),
                    date == today,
                    byDate.TryGetValue(date, out var entries) ? entries : []);
            })
            .ToList();
    }

    /// <summary>Gibt es überhaupt Spiele in der Woche?</summary>
    public static bool HasMatches(IReadOnlyList<FootballWeekDay> week) => week.Any(d => d.Entries.Count > 0);

    private static DateOnly BerlinDate(DateTimeOffset utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, BerlinTz).DateTime);
}
