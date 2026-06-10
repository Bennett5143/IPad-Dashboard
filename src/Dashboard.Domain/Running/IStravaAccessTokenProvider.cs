namespace Dashboard.Domain.Running;

/// <summary>
/// Liefert ein gültiges Strava-Access-Token (refresht bei Bedarf transparent). Gibt <c>null</c>,
/// wenn (noch) keine Verbindung besteht – Trennung von Token-Verwaltung und API-Aufrufen.
/// </summary>
public interface IStravaAccessTokenProvider
{
    Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default);
}
