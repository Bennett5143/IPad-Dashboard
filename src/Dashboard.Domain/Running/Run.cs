namespace Dashboard.Domain.Running;

/// <summary>
/// Ein abgeschlossener Lauf in der Domänen-Sicht – inkl. dekodiertem GPS-Track als Punktliste.
/// Bewusst frei von NetTopologySuite/EF; die Persistenz-Abbildung (LineString) ist Infrastruktur.
/// </summary>
/// <remarks>
/// Die Aktivitätsmetriken (ab FA-8.14) sind optional, damit bestehende Aufrufer unverändert
/// bleiben; sie speisen Year-in-Review (FA-8.16) und die Tageszeit-Auswertungen (FA-10.01).
/// </remarks>
public sealed record Run(
    long Id,
    string Name,
    string Type,
    DateTimeOffset StartUtc,
    double DistanceMeters,
    TimeSpan MovingTime,
    IReadOnlyList<GeoPoint> Track,
    StravaStreams? Streams = null,
    double? ElevationGainMeters = null,
    int? AverageHeartRate = null,
    int? MaxHeartRate = null);
