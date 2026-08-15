using Dashboard.Domain.Enums;
using Dashboard.Domain.ValueObjects;

namespace Dashboard.Domain.Whoop;

/// <summary>
/// Bildet WHOOP-Workouts auf Habit-Typen ab (reine, testbare Logik). Die Sportart bestimmt
/// <see cref="WhoopSportClassifier"/>; hier steht nur, welche davon eine geführte Gewohnheit
/// abhakt. Sportarten ohne geführte Gewohnheit — Seilspringen, Dehnen/Yoga — ergeben
/// <c>null</c>, statt im Hintergrund unsichtbare Häkchen zu setzen (siehe
/// <see cref="Habits.HabitCatalog"/>).
/// </summary>
public static class WhoopHabitMapper
{
    /// <summary>Ab diesem Zeitanteil in Zone 4+5 gilt ein Lauf als VO2max-Intervalle statt Zone 2.</summary>
    public const double HighIntensityThreshold = 0.15;

    public static HabitKind? MapKind(WhoopWorkout workout)
    {
        var kind = WhoopSportClassifier.Classify(workout) switch
        {
            WhoopSport.Running => workout.HighIntensityShare >= HighIntensityThreshold
                ? HabitKind.Vo2MaxIntervals
                : HabitKind.Zone2Run,
            WhoopSport.Strength => HabitKind.Strength,
            _ => (HabitKind?)null,
        };

        return kind is { } candidate && Habits.HabitCatalog.IsActive(candidate) ? candidate : null;
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
}
