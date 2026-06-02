namespace Dashboard.Domain.Habits;

public static class HabitWeek
{
    public static (DateOnly Start, DateOnly End) ContainingDate(DateOnly date)
    {
        // DayOfWeek: Sonntag = 0, Montag = 1, …, Samstag = 6.
        // Wir wollen Mo–So → tracke Tage seit dem letzten Montag.
        var daysSinceMonday = date.DayOfWeek == DayOfWeek.Sunday
            ? 6
            : (int)date.DayOfWeek - 1;

        var monday = date.AddDays(-daysSinceMonday);
        return (monday, monday.AddDays(6));
    }
}