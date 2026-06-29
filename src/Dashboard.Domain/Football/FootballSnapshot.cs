namespace Dashboard.Domain.Football;

/// <summary>
/// UI-fertige Sicht auf alle konfigurierten Vereine. Wird in <see cref="FootballState"/> gehalten.
/// <paramref name="LeagueTables"/> (Top-5-Ligen) und <paramref name="InterestingGamesThisWeek"/> kamen
/// in 15.6 dazu; beide sind optional, damit ältere Aufrufer/Tests unverändert kompilieren.
/// </summary>
public sealed record FootballSnapshot(
    IReadOnlyList<FootballTeamSnapshot> Teams,
    DateTimeOffset RetrievedAtUtc,
    IReadOnlyList<LeagueTable>? LeagueTables = null,
    int InterestingGamesThisWeek = 0) : Common.ISnapshot;
