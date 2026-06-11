using Dashboard.Domain.Enums;
using Dashboard.Domain.ValueObjects;

namespace Dashboard.Domain.Whoop;

/// <summary>
/// Bildet WHOOP-Workouts auf Habit-Typen ab (reine, testbare Logik). Mapping über den Sportnamen
/// (robust gegen die WHOOP-Umstellung von numerischen sport_ids auf sport_name-Strings).
/// </summary>
public static class WhoopHabitMapper
{
    /// <summary>Ab diesem Zeitanteil in Zone 4+5 gilt ein Lauf als VO2max-Intervalle statt Zone 2.</summary>
    public const double HighIntensityThreshold = 0.15;

    public static HabitKind? MapKind(WhoopWorkout workout)
    {
        var sport = Normalize(workout.Sport);

        if (sport.Contains("run", StringComparison.Ordinal))
        {
            return workout.HighIntensityShare >= HighIntensityThreshold
                ? HabitKind.Vo2MaxIntervals
                : HabitKind.Zone2Run;
        }

        if (sport.Contains("rope", StringComparison.Ordinal))
        {
            return HabitKind.JumpRope;
        }

        // Sowohl normales Krafttraining („weightlifting") als auch funktionelles/EMOM
        // („functional fitness") sind im Tracker HabitKind.Strength; EMOM-Segmente bleiben manuell.
        if (sport.Contains("weight", StringComparison.Ordinal)
            || sport.Contains("strength", StringComparison.Ordinal)
            || sport.Contains("functional", StringComparison.Ordinal)
            || sport.Contains("powerlifting", StringComparison.Ordinal)
            || sport.Contains("bodybuilding", StringComparison.Ordinal))
        {
            return HabitKind.Strength;
        }

        if (sport.Contains("yoga", StringComparison.Ordinal)
            || sport.Contains("pilates", StringComparison.Ordinal)
            || sport.Contains("stretch", StringComparison.Ordinal)
            || sport.Contains("mobility", StringComparison.Ordinal))
        {
            return HabitKind.Stretching;
        }

        return null;
    }

    /// <summary>Dauer (Minuten) + Pace (min/km) aus einem Lauf-Workout; <c>null</c> ohne valide Distanz/Dauer.</summary>
    public static RunningDetails? BuildRunningDetails(WhoopWorkout workout)
    {
        var minutes = (int)Math.Round(workout.Duration.TotalMinutes, MidpointRounding.AwayFromZero);
        if (minutes <= 0 || workout.DistanceMeters is not > 0)
        {
            return null;
        }

        var km = workout.DistanceMeters.Value / 1000.0;
        var pace = (decimal)Math.Round(workout.Duration.TotalMinutes / km, 2, MidpointRounding.AwayFromZero);
        return pace <= 0 ? null : new RunningDetails(minutes, pace);
    }

    private static string Normalize(string? sport) =>
        (sport ?? string.Empty).Replace('_', ' ').Trim().ToLowerInvariant();
}
