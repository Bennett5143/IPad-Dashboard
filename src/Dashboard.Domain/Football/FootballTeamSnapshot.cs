namespace Dashboard.Domain.Football;

/// <summary>
/// Aggregierte Sicht auf einen Verein: jüngste Ergebnisse, nächste Spiele, Tabellenplatz und
/// (für die Tap-Ansicht, FA-4.06) die vollständige Ligatabelle.
/// </summary>
public sealed record FootballTeamSnapshot(
    string TeamName,
    IReadOnlyList<Match> RecentResults,
    IReadOnlyList<Match> Upcoming,
    TablePosition? Standing,
    IReadOnlyList<LeagueRow>? Table = null);
