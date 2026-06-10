namespace Dashboard.Domain.Running;

/// <summary>
/// OAuth2-Token-Tripel von Strava. <see cref="ExpiresAtUtc"/> ist der absolute Ablauf des Access-Tokens.
/// Bei jedem Refresh liefert Strava ein neues Refresh-Token – immer das neueste persistieren.
/// </summary>
public sealed record StravaTokenSet(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc)
{
    /// <summary>Strava erneuert Tokens, die in ≤ 1 h ablaufen – wir refreshen mit demselben Puffer.</summary>
    public bool NeedsRefresh(DateTimeOffset nowUtc) => ExpiresAtUtc - nowUtc <= TimeSpan.FromHours(1);
}
