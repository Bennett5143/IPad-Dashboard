namespace Dashboard.Domain.Whoop;

/// <summary>Grobe Art eines WHOOP-Workouts, aus dem Sportnamen abgeleitet.</summary>
public enum WhoopSport
{
    Other,
    Running,
    Strength,
    JumpRope,
    Mobility
}

/// <summary>
/// Ordnet ein WHOOP-Workout seiner Sportart zu — über den Sportnamen, robust gegen die WHOOP-
/// Umstellung von numerischen <c>sport_id</c>s auf <c>sport_name</c>-Strings.
/// <para>
/// Bewusst getrennt vom Habit-Mapping: „welche Sportart ist das" und „welche Gewohnheit hakt das
/// ab" sind zwei Fragen. Als sie noch eine waren, nahm das Abwählen einer Gewohnheit der
/// WHOOP-Auswertung ihre Kategorie mit — die wertet Workouts aus, nicht Habits.
/// </para>
/// </summary>
public static class WhoopSportClassifier
{
    public static WhoopSport Classify(WhoopWorkout workout) => Classify(workout.Sport);

    public static WhoopSport Classify(string? sportName)
    {
        var sport = (sportName ?? string.Empty).Replace('_', ' ').Trim().ToLowerInvariant();

        if (sport.Contains("run", StringComparison.Ordinal))
        {
            return WhoopSport.Running;
        }

        if (sport.Contains("rope", StringComparison.Ordinal))
        {
            return WhoopSport.JumpRope;
        }

        // Normales Krafttraining („weightlifting") und funktionelles/EMOM („functional fitness")
        // fallen zusammen; EMOM-Segmente bleiben manuell.
        if (sport.Contains("weight", StringComparison.Ordinal)
            || sport.Contains("strength", StringComparison.Ordinal)
            || sport.Contains("functional", StringComparison.Ordinal)
            || sport.Contains("powerlifting", StringComparison.Ordinal)
            || sport.Contains("bodybuilding", StringComparison.Ordinal))
        {
            return WhoopSport.Strength;
        }

        if (sport.Contains("yoga", StringComparison.Ordinal)
            || sport.Contains("pilates", StringComparison.Ordinal)
            || sport.Contains("stretch", StringComparison.Ordinal)
            || sport.Contains("mobility", StringComparison.Ordinal))
        {
            return WhoopSport.Mobility;
        }

        return WhoopSport.Other;
    }
}
