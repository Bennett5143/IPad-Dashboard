namespace Dashboard.Domain.Whoop;

/// <summary>
/// Ein WHOOP-Workout. <see cref="Sport"/> ist der (klein geschriebene) WHOOP-Sportname;
/// <see cref="HighIntensityShare"/> ist der Zeitanteil in den HF-Zonen 4+5 (0..1) – damit lässt
/// sich ein Zone-2-Lauf von VO2max-Intervallen unterscheiden.
/// </summary>
/// <remarks>
/// Die Belastungsfelder (ab FA-9.12) sind optional, damit bestehende Aufrufer unverändert
/// bleiben; sie definieren zugleich das Zielschema der Workout-Persistenz und der
/// Tageszeit-Auswertungen (FA-10.01).
/// </remarks>
public sealed record WhoopWorkout(
    string Id,
    string Sport,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    double? DistanceMeters,
    double HighIntensityShare,
    double? Strain = null,
    double? Kilojoule = null,
    int? AverageHeartRate = null,
    int? MaxHeartRate = null,
    WhoopZoneTimes? Zones = null)
{
    public TimeSpan Duration => EndUtc - StartUtc;
}

/// <summary>Zeit (ms) je HF-Zone 0–5 eines Workouts – Grundlage für „Zeit in Zonen"-Auswertungen.</summary>
public sealed record WhoopZoneTimes(
    long Zone0Milli,
    long Zone1Milli,
    long Zone2Milli,
    long Zone3Milli,
    long Zone4Milli,
    long Zone5Milli);
