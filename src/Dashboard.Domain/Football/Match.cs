namespace Dashboard.Domain.Football;

/// <summary>
/// Ein Spiel aus Sicht des konfigurierten Vereins ("wir"). Gegner, Heim/Auswärts und Tore
/// sind bereits perspektivisch aufgelöst, damit die UI nichts mehr umrechnen muss.
/// Tore sind <c>null</c>, solange das Spiel nicht angepfiffen/beendet ist.
/// <para>
/// <paramref name="Matchday"/> und <paramref name="Stage"/> kamen mit dem Wochenkalender dazu: er
/// fasst einen Champions-League-Spieltag zu einem Eintrag zusammen und braucht dafür dessen Namen.
/// Beide sind optional — nicht jede Quelle liefert sie, und ohne sie bleibt der Eintrag lesbar.
/// </para>
/// </summary>
public sealed record Match(
    DateTimeOffset KickoffUtc,
    string CompetitionCode,
    string Opponent,
    bool IsHome,
    int? OwnGoals,
    int? OpponentGoals,
    int? Matchday = null,
    string? Stage = null)
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
