using NetTopologySuite.Geometries;

namespace Dashboard.Infrastructure.Strava;

/// <summary>Persistenz-Entity eines Laufs. <see cref="Route"/> ist eine PostGIS-<c>geometry(LineString,4326)</c>.</summary>
internal sealed class RunActivityEntity
{
    public long Id { get; set; }              // Strava-Activity-Id
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset StartUtc { get; set; }
    public double DistanceMeters { get; set; }
    public int MovingTimeSeconds { get; set; }
    public LineString? Route { get; set; }

    // Pro-Punkt-Streams (index-aligned mit Route.Coordinates), erst nach dem Backfill befüllt.
    public bool StreamsFetched { get; set; }
    public int[]? TimeOffsetsSeconds { get; set; }
    public double[]? AltitudesMeters { get; set; }
    public int[]? HeartRates { get; set; }
}

/// <summary>Single-Row-Entity (Id = 1) mit dem aktuellen OAuth-Token-Satz.</summary>
internal sealed class StravaTokenEntity
{
    public int Id { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

/// <summary>Single-Row-Entity (Id = 1) mit dem Sync-Zustand.</summary>
internal sealed class SyncStateEntity
{
    public int Id { get; set; }
    public DateTimeOffset? LastSuccessfulSyncUtc { get; set; }
    public DateTimeOffset? LastAttemptUtc { get; set; }
    public string? LastError { get; set; }
}
