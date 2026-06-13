namespace Dashboard.Domain.Habits;

/// <summary>Aktuelle und längste (historisch im betrachteten Zeitraum) Serie.</summary>
public sealed record StreakResult(int Current, int Longest);

/// <summary>
/// Habit-Serien (FA-3.09) — reine, testbare Logik. Bewusst zwei Maße:
/// • <b>Wochen-Serie je Habit</b> (Wochen Mo–So mit ≥ 1 Eintrag) — Tages-Serien wären bei
///   3×/Woche-Habits sinnfrei (Ruhetage brechen sie).
/// • <b>globale Tages-Serie</b> (Tage mit irgendeinem Habit).
/// Karenz: die laufende Woche bzw. der heutige Tag brechen die Serie nicht, solange sie nur
/// „noch nicht" erfüllt sind (sonst bräche jede Serie jeden Wochen-/Tagesanfang ab).
/// </summary>
public static class HabitStreakCalculator
{
    /// <summary>Aufeinanderfolgende Wochen mit mindestens einem Eintrag.</summary>
    public static StreakResult WeeklyStreak(IReadOnlySet<DateOnly> doneDates, DateOnly today)
    {
        var weeks = doneDates.Select(d => HabitWeek.ContainingDate(d).Start).ToHashSet();
        var thisMonday = HabitWeek.ContainingDate(today).Start;

        // Laufende Woche ohne Eintrag → bei der Vorwoche beginnen (Karenz).
        var cursor = weeks.Contains(thisMonday) ? thisMonday : thisMonday.AddDays(-7);
        var current = 0;
        while (weeks.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-7);
        }

        return new StreakResult(current, LongestConsecutive(weeks, stepDays: 7));
    }

    /// <summary>Aufeinanderfolgende Tage mit irgendeinem Habit.</summary>
    public static StreakResult DailyStreak(IEnumerable<DateOnly> anyDoneDates, DateOnly today)
    {
        var days = anyDoneDates.ToHashSet();

        var cursor = days.Contains(today) ? today : today.AddDays(-1); // Karenz für „heute noch offen"
        var current = 0;
        while (days.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-1);
        }

        return new StreakResult(current, LongestConsecutive(days, stepDays: 1));
    }

    private static int LongestConsecutive(IReadOnlySet<DateOnly> points, int stepDays)
    {
        if (points.Count == 0)
        {
            return 0;
        }

        var sorted = points.OrderBy(d => d).ToList();
        int longest = 1, run = 1;
        for (var i = 1; i < sorted.Count; i++)
        {
            run = sorted[i] == sorted[i - 1].AddDays(stepDays) ? run + 1 : 1;
            longest = Math.Max(longest, run);
        }

        return longest;
    }
}
