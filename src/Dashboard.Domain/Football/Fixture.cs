namespace Dashboard.Domain.Football;

/// <summary>Grober Spielstatus, perspektiv-neutral aus dem football-data-Status abgeleitet.</summary>
public enum MatchStatus
{
    Scheduled,
    Live,
    Finished
}

/// <summary>Minimaler Team-Verweis (ID + Name + Kürzel) für neutrale Spiel-/Bracket-Sichten.</summary>
public readonly record struct TeamRef(int Id, string Name, string? Tla);

/// <summary>
/// Perspektiv-neutrales Spiel (anders als <see cref="Match"/>, das aus Vereinssicht aufgelöst ist).
/// Trägt Stage/Gruppe und die On-Pitch-Tore (regulär + Verlängerung, OHNE Elfmeterschießen) plus
/// das Elfmeterschießen separat – Grundlage für Gruppen-Tabellen und den K.o.-Bracket (15.7).
/// </summary>
public sealed record Fixture(
    DateTimeOffset KickoffUtc,
    string Stage,
    string? Group,
    TeamRef Home,
    TeamRef Away,
    int? HomeGoals,
    int? AwayGoals,
    int? HomePenalties,
    int? AwayPenalties,
    bool AfterExtraTime,
    MatchStatus Status)
{
    /// <summary>Beide On-Pitch-Tore bekannt (Spiel gespielt).</summary>
    public bool IsPlayed => HomeGoals.HasValue && AwayGoals.HasValue;

    /// <summary>Das Spiel/der Zweikampf wurde im Elfmeterschießen entschieden.</summary>
    public bool WentToPenalties => HomePenalties.HasValue && AwayPenalties.HasValue;

    /// <summary>Beide Teams stehen fest (kein „TBD" einer noch nicht ausgespielten Runde).</summary>
    public bool TeamsKnown => Home.Id != 0 && Away.Id != 0;
}
