namespace Dashboard.Domain.Whoop;

/// <summary>
/// Ein WHOOP-Workout, reduziert auf das für die Habit-Übernahme Nötige. <see cref="Sport"/> ist der
/// (klein geschriebene) WHOOP-Sportname; <see cref="HighIntensityShare"/> ist der Zeitanteil in den
/// HF-Zonen 4+5 (0..1) – damit lässt sich ein Zone-2-Lauf von VO2max-Intervallen unterscheiden.
/// </summary>
public sealed record WhoopWorkout(
    string Id,
    string Sport,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    double? DistanceMeters,
    double HighIntensityShare)
{
    public TimeSpan Duration => EndUtc - StartUtc;
}
