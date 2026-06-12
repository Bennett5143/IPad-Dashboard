namespace Dashboard.Infrastructure.Whoop;

/// <summary>
/// Konfiguration der WHOOP-Anbindung. <see cref="ClientId"/>/<see cref="ClientSecret"/> sind
/// Geheimnisse (User-Secrets). OAuth2 nach https://developer.whoop.com/docs/developing/oauth/.
/// </summary>
public sealed class WhoopOptions
{
    public const string SectionName = "Whoop";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Muss exakt der in der WHOOP-App hinterlegten Redirect-URL entsprechen.
    /// WHOOP akzeptiert nur <c>https://</c> (kein <c>http://</c>) – daher der https-Dev-Port (Profil „https").
    /// </summary>
    public string RedirectUri { get; init; } = "https://localhost:7204/whoop/callback";

    /// <summary><c>offline</c> ist für Refresh-Tokens nötig; restliche Read-Scopes für Daten.</summary>
    public string Scope { get; init; } = "offline read:recovery read:sleep read:cycles read:workout read:profile";

    public string BaseUrl { get; init; } = "https://api.prod.whoop.com/";

    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Wie weit der historische Backfill der Tageswerte zurückreicht (FA-9.10);
    /// <c>0</c> deaktiviert ihn. Geholt wird gefenstert, ein Fenster pro Sync-Zyklus.
    /// </summary>
    public int BackfillDays { get; init; } = 365;
}
