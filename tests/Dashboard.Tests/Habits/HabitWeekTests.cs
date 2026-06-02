namespace Dashboard.Tests.Habits;

public class HabitWeekTests
{
    [Theory]
    [InlineData(2026, 5, 18, "2026-05-18", "2026-05-24")] // Montag selbst
    [InlineData(2026, 5, 20, "2026-05-18", "2026-05-24")] // Mittwoch
    [InlineData(2026, 5, 24, "2026-05-18", "2026-05-24")] // Sonntag
    [InlineData(2026, 1, 1, "2025-12-29", "2026-01-04")]  // Jahreswechsel
    public void ContainingDate_ReturnsMondayToSunday(int y, int m, int d, string expectedStart, string expectedEnd)
    {
        var (start, end) = HabitWeek.ContainingDate(new DateOnly(y, m, d));

        Assert.Equal(DateOnly.Parse(expectedStart), start);
        Assert.Equal(DateOnly.Parse(expectedEnd), end);
        Assert.Equal(DayOfWeek.Monday, start.DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, end.DayOfWeek);
    }
}