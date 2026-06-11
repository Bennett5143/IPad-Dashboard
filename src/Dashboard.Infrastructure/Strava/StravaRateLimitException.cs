namespace Dashboard.Infrastructure.Strava;

/// <summary>
/// Strava-Rate-Limit (HTTP 429) oder vorübergehender Serverfehler (5xx) – der Stream-Backfill
/// pausiert dann und versucht es im nächsten Sync-Zyklus erneut.
/// </summary>
public sealed class StravaRateLimitException(string message) : Exception(message);
