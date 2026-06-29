namespace Dashboard.Domain.Football;

/// <summary>
/// Champions-League-Sicht (15.7): die 36er-Ligaphase als Tabelle und der daraus/aus den K.o.-Spielen
/// abgeleitete Bracket. Beide Teile sind optional (best-effort je nach API-Verfügbarkeit).
/// </summary>
public sealed record ChampionsLeague(
    LeagueTable? LeaguePhase,
    KnockoutBracket? Bracket);

/// <summary>
/// Turnier-Sicht (EM/WM): Gruppentabellen (eine <see cref="LeagueTable"/> je Gruppe) und der K.o.-Baum.
/// </summary>
public sealed record TournamentView(
    string Code,
    string Name,
    IReadOnlyList<LeagueTable> Groups,
    KnockoutBracket? Bracket);
