namespace Dashboard.Domain.Whoop;

/// <summary>
/// OAuth2-Token-Satz von WHOOP. <see cref="ExpiresAtUtc"/> ist der absolute Ablauf des Access-Tokens.
/// WHOOP rotiert bei jedem Refresh <b>beide</b> Tokens – immer den neuesten Satz persistieren.
/// </summary>
public sealed record WhoopTokenSet(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc)
{
    /// <summary>
    /// WHOOP-Access-Tokens leben nur ~1 h; daher kleiner Puffer (5 min) statt 1 h wie bei Strava,
    /// sonst würde permanent refresht.
    /// </summary>
    public bool NeedsRefresh(DateTimeOffset nowUtc) => ExpiresAtUtc - nowUtc <= TimeSpan.FromMinutes(5);
}
