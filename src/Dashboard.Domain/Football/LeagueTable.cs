namespace Dashboard.Domain.Football;

/// <summary>
/// Vollständige Ligatabelle eines Wettbewerbs (FA-4.06, Erweiterung 15.6). <see cref="LeagueRow.IsOwnTeam"/>
/// ist für jeden in dieser Liga getrackten Verein gesetzt (mehrere möglich, anders als in der
/// vereins-zentrierten <see cref="FootballTeamSnapshot.Table"/>).
/// </summary>
public sealed record LeagueTable(
    string Code,
    string Name,
    IReadOnlyList<LeagueRow> Rows);
