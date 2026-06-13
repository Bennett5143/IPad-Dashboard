using Dashboard.Domain.Habits;

namespace Dashboard.Tests.Habits;

public class HabitStreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 6, 12); // Freitag
    private static readonly DateOnly ThisMonday = new(2026, 6, 8);

    private static IReadOnlySet<DateOnly> Dates(params DateOnly[] d) => d.ToHashSet();

    [Fact]
    public void WeeklyStreak_CountsConsecutiveWeeks_WithCurrentWeekDone()
    {
        // Eintrag in dieser, letzter und vorletzter Woche.
        var dates = Dates(Today, ThisMonday.AddDays(-7), ThisMonday.AddDays(-14));

        var streak = HabitStreakCalculator.WeeklyStreak(dates, Today);

        Assert.Equal(3, streak.Current);
        Assert.Equal(3, streak.Longest);
    }

    [Fact]
    public void WeeklyStreak_GraceForEmptyCurrentWeek()
    {
        // Diese Woche noch leer, aber die letzten beiden Wochen aktiv → Serie bleibt 2.
        var dates = Dates(ThisMonday.AddDays(-3), ThisMonday.AddDays(-10));

        Assert.Equal(2, HabitStreakCalculator.WeeklyStreak(dates, Today).Current);
    }

    [Fact]
    public void WeeklyStreak_BreaksAfterTwoEmptyWeeks()
    {
        // Letzte Aktivität vor 2 Wochen (Lücke = diese + letzte Woche leer) → aktuell 0.
        var dates = Dates(ThisMonday.AddDays(-14));

        var streak = HabitStreakCalculator.WeeklyStreak(dates, Today);
        Assert.Equal(0, streak.Current);
        Assert.Equal(1, streak.Longest);
    }

    [Fact]
    public void WeeklyStreak_LongestIgnoresGaps()
    {
        var dates = Dates(
            ThisMonday, ThisMonday.AddDays(-7),                       // aktuelle Serie 2
            ThisMonday.AddDays(-35), ThisMonday.AddDays(-42), ThisMonday.AddDays(-49)); // frühere Serie 3

        var streak = HabitStreakCalculator.WeeklyStreak(dates, Today);
        Assert.Equal(2, streak.Current);
        Assert.Equal(3, streak.Longest);
    }

    [Fact]
    public void DailyStreak_CountsConsecutiveDays_WithTodayDone()
    {
        var dates = Dates(Today, Today.AddDays(-1), Today.AddDays(-2));

        var streak = HabitStreakCalculator.DailyStreak(dates, Today);
        Assert.Equal(3, streak.Current);
        Assert.Equal(3, streak.Longest);
    }

    [Fact]
    public void DailyStreak_GraceForToday_ButBreaksIfYesterdayMissing()
    {
        // Heute offen, gestern + vorgestern erledigt → Serie 2 (Karenz für heute).
        Assert.Equal(2, HabitStreakCalculator.DailyStreak(
            Dates(Today.AddDays(-1), Today.AddDays(-2)), Today).Current);

        // Heute UND gestern offen → Serie 0.
        Assert.Equal(0, HabitStreakCalculator.DailyStreak(
            Dates(Today.AddDays(-2)), Today).Current);
    }

    [Fact]
    public void Streaks_EmptyInput()
    {
        Assert.Equal(new StreakResult(0, 0), HabitStreakCalculator.WeeklyStreak(Dates(), Today));
        Assert.Equal(new StreakResult(0, 0), HabitStreakCalculator.DailyStreak([], Today));
    }
}
