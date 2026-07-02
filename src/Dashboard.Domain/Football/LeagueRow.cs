namespace Dashboard.Domain.Football;

/// <summary>Eine Zeile der vollständigen Ligatabelle (FA-4.06).</summary>
public sealed record LeagueRow(
    int Position,
    string TeamName,
    string? ShortCode,
    int PlayedGames,
    int Won,
    int Draw,
    int Lost,
    int GoalDifference,
    int Points,
    bool IsOwnTeam,
    // Upstream-URL des Vereinswappens (bei Nationalteams die Flagge). Wird NICHT direkt vom
    // Browser geladen (Offline-Kiosk), sondern über den server-seitigen /crests-Proxy.
    string? CrestUrl = null);
