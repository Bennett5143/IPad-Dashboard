namespace Dashboard.Domain.Football;

/// <summary>
/// Ein K.o.-Zweikampf (ein Einzelspiel wie bei WM/EM/Finale oder Hin-/Rückspiel wie in der CL).
/// <see cref="HomeAggregate"/>/<see cref="AwayAggregate"/> sind die summierten On-Pitch-Tore aus
/// Sicht der Leg-1-Heimmannschaft; ein eventuelles Elfmeterschießen steht getrennt.
/// </summary>
public sealed record KnockoutTie(
    string Stage,
    TeamRef Home,
    TeamRef Away,
    int? HomeAggregate,
    int? AwayAggregate,
    int? HomePenalties,
    int? AwayPenalties,
    TeamRef? Winner,
    bool TwoLegs,
    bool Decided,
    DateTimeOffset EarliestKickoff);

/// <summary>Eine K.o.-Runde (z. B. Achtelfinale) mit ihren Zweikämpfen.</summary>
public sealed record KnockoutRound(
    string Stage,
    string Label,
    IReadOnlyList<KnockoutTie> Ties);

/// <summary>Der K.o.-Baum eines Wettbewerbs, Runden in Spielreihenfolge (frühe Runde zuerst).</summary>
public sealed record KnockoutBracket(IReadOnlyList<KnockoutRound> Rounds);
