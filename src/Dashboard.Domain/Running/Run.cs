namespace Dashboard.Domain.Running;

/// <summary>
/// Ein abgeschlossener Lauf in der Domänen-Sicht – inkl. dekodiertem GPS-Track als Punktliste.
/// Bewusst frei von NetTopologySuite/EF; die Persistenz-Abbildung (LineString) ist Infrastruktur.
/// </summary>
public sealed record Run(
    long Id,
    string Name,
    string Type,
    DateTimeOffset StartUtc,
    double DistanceMeters,
    TimeSpan MovingTime,
    IReadOnlyList<GeoPoint> Track,
    StravaStreams? Streams = null);
