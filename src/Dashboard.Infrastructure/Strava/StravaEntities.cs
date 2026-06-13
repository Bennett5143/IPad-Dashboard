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

    // Aktivitätsmetriken aus der Listen-Antwort (FA-8.14); fehlen bei Läufen ohne
    // Höhenprofil/HF-Messung.
    public double? ElevationGainMeters { get; set; }
    public int? AverageHeartRate { get; set; }
    public int? MaxHeartRate { get; set; }

    // Pro-Punkt-Streams (index-aligned mit Route.Coordinates), erst nach dem Backfill befüllt.
    public bool StreamsFetched { get; set; }
    public int[]? TimeOffsetsSeconds { get; set; }
    public double[]? AltitudesMeters { get; set; }
    public int[]? HeartRates { get; set; }

    // Routen-Erkennung (FA-8.17): Cluster-Zuordnung; RouteMatchedUtc = bearbeitet (auch ohne Match).
    public int? RouteClusterId { get; set; }
    public DateTimeOffset? RouteMatchedUtc { get; set; }
}

/// <summary>Eine erkannte „Standard-Runde" (FA-8.17); Repräsentant ist der erste zugeordnete Lauf.</summary>
internal sealed class RouteClusterEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long RepresentativeRunId { get; set; }
    public double RepresentativeDistanceMeters { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
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

    /// <summary>Wann der einmalige Voll-Re-Sync der Aktivitätsmetriken lief (FA-8.14); <c>null</c> = steht aus.</summary>
    public DateTimeOffset? DetailsBackfilledUtc { get; set; }
}
