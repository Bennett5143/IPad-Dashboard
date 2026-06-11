namespace Dashboard.Domain.Running;

/// <summary>
/// Pro-Punkt-Streams einer Aktivität (Strava), index-aligned mit <see cref="Track"/> (volle Auflösung
/// aus dem latlng-Stream). <see cref="AltitudesMeters"/>/<see cref="HeartRates"/> sind <c>null</c>,
/// wenn die Aktivität diese Reihe nicht hat (z. B. kein Höhen- oder Pulssensor).
/// </summary>
public sealed record StravaStreams(
    IReadOnlyList<GeoPoint> Track,
    IReadOnlyList<int> TimeOffsetsSeconds,
    IReadOnlyList<double>? AltitudesMeters,
    IReadOnlyList<int>? HeartRates);
