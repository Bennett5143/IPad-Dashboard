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

    // Orts-Zuordnung; PlaceAssignedUtc = bearbeitet (auch ohne Ort, etwa ohne Strecke).
    public int? PlaceId { get; set; }
    public DateTimeOffset? PlaceAssignedUtc { get; set; }
}

/// <summary>
/// Ein Ort, an dem gelaufen wird. Mittelpunkt = Mittel der Startpunkte, die Grenzen umschließen
/// alle Strecken des Ortes und tragen später den Kartenausschnitt und das Kachel-Warmup.
/// </summary>
internal sealed class RunPlaceEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CentreLatitude { get; set; }
    public double CentreLongitude { get; set; }
    public double MinLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MaxLongitude { get; set; }
    public int RunCount { get; set; }
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
