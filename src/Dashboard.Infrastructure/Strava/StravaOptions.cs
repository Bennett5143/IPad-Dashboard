namespace Dashboard.Infrastructure.Strava;

/// <summary>
/// Konfiguration der Strava-Anbindung. <see cref="ClientId"/>/<see cref="ClientSecret"/> sind
/// Geheimnisse (User-Secrets). OAuth2 nach https://developers.strava.com/docs/authentication/.
/// </summary>
public sealed class StravaOptions
{
    public const string SectionName = "Strava";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>Muss exakt der in der Strava-App hinterlegten Callback-URL entsprechen.</summary>
    public string RedirectUri { get; init; } = "http://localhost:5235/strava/callback";

    /// <summary><c>activity:read_all</c> auch für private Aktivitäten.</summary>
    public string Scope { get; init; } = "activity:read_all";

    public string BaseUrl { get; init; } = "https://www.strava.com/";

    public int PerPage { get; init; } = 200;

    public TimeSpan SyncInterval { get; init; } = TimeSpan.FromMinutes(30);
}
