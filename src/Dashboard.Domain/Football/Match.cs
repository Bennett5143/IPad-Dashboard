namespace Dashboard.Domain.Football;

/// <summary>
/// Ein Spiel aus Sicht des konfigurierten Vereins ("wir"). Gegner, Heim/Auswärts und Tore
/// sind bereits perspektivisch aufgelöst, damit die UI nichts mehr umrechnen muss.
/// Tore sind <c>null</c>, solange das Spiel nicht angepfiffen/beendet ist.
/// </summary>
public sealed record Match(
    DateTimeOffset KickoffUtc,
    string Competition,
    string Opponent,
    bool IsHome,
    int? OwnGoals,
    int? OpponentGoals)
{
    public bool IsFinished => OwnGoals.HasValue && OpponentGoals.HasValue;

    public MatchOutcome? Outcome => !IsFinished
        ? null
        : OwnGoals!.Value > OpponentGoals!.Value
            ? MatchOutcome.Win
            : OwnGoals.Value == OpponentGoals.Value
                ? MatchOutcome.Draw
                : MatchOutcome.Loss;
}
