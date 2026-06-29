namespace Dashboard.Domain.Football;

/// <summary>
/// Perspektiv-neutraler, ID-basierter Schlüssel eines Spiels für den Wochen-Zähler. IDs statt Namen,
/// weil der konfigurierte Vereinsname ≠ API-Name sein kann und ein Spiel zweier getrackter Vereine
/// sonst nicht entdoppelt würde.
/// </summary>
public readonly record struct FixtureKey(
    string CompetitionCode,
    DateTimeOffset KickoffUtc,
    int HomeTeamId,
    int AwayTeamId);

/// <summary>
/// Zählt die „interessanten" Spiele der aktuellen Woche (Mo–So, Europe/Berlin) aus einer Fixture-Menge
/// (FA-4.05). Dedupliziert über (Wettbewerb, Anstoßzeit, ungeordnetes Team-ID-Paar), damit dasselbe
/// Spiel – aus Sicht zweier getrackter Vereine doppelt geliefert – nur einmal zählt. Rein und testbar.
/// </summary>
public static class FootballWeekCounter
{
    private static readonly TimeZoneInfo BerlinTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static int CountInterestingGames(IEnumerable<FixtureKey> fixtures, DateTimeOffset nowUtc)
    {
        var today = BerlinDate(nowUtc);
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); // Mo-basiert
        var sunday = monday.AddDays(6);

        var seen = new HashSet<(string, long, int, int)>();
        var count = 0;
        foreach (var fixture in fixtures)
        {
            var date = BerlinDate(fixture.KickoffUtc);
            if (date < monday || date > sunday)
            {
                continue;
            }

            var lo = Math.Min(fixture.HomeTeamId, fixture.AwayTeamId);
            var hi = Math.Max(fixture.HomeTeamId, fixture.AwayTeamId);
            if (seen.Add((fixture.CompetitionCode, fixture.KickoffUtc.UtcTicks, lo, hi)))
            {
                count++;
            }
        }

        return count;
    }

    private static DateOnly BerlinDate(DateTimeOffset utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, BerlinTz).DateTime);
}
