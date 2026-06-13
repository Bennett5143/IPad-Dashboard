using Dashboard.Web.Components.Pages;

namespace Dashboard.Tests.Components.Pages;

public class HabitsHeatmapBuilderTests
{
    private static readonly DateOnly Today = new(2026, 6, 12); // Freitag

    private static IReadOnlyDictionary<HabitKind, IReadOnlySet<DateOnly>> Done(
        params (HabitKind Kind, DateOnly[] Dates)[] entries) =>
        entries.ToDictionary(e => e.Kind, e => (IReadOnlySet<DateOnly>)e.Dates.ToHashSet());

    [Fact]
    public void Bucket_MapsLevelsToZeroToFour()
    {
        Assert.Equal(0, HabitsHeatmapBuilder.Bucket(0, 5));
        Assert.Equal(4, HabitsHeatmapBuilder.Bucket(1, 1));   // binär: erledigt → voll
        Assert.Equal(0, HabitsHeatmapBuilder.Bucket(0, 1));
        Assert.Equal(1, HabitsHeatmapBuilder.Bucket(1, 5));
        Assert.Equal(4, HabitsHeatmapBuilder.Bucket(5, 5));
    }

    [Fact]
    public void Build_SingleKind_MarksDoneDays()
    {
        var done = Done((HabitKind.Strength, [Today, Today.AddDays(-1)]));

        var view = HabitsHeatmapBuilder.Build(done, HabitKind.Strength, Today);

        // Genau zwei Zellen mit Bucket > 0.
        Assert.Equal(2, view.Cells.Count(c => c.Bucket > 0));
        Assert.All(view.Cells.Where(c => c.Bucket > 0), c => Assert.Equal(4, c.Bucket));
        // Heute (Freitag) ist Wochentag 4 (Mo=0); Zelle existiert.
        Assert.Contains(view.Cells, c => c.Weekday == 4 && c.Bucket == 4);
    }

    [Fact]
    public void Build_AllKinds_IntensityIsCountOfHabits()
    {
        var done = Done(
            (HabitKind.Strength, [Today]),
            (HabitKind.Zone2Run, [Today]),
            (HabitKind.JumpRope, [Today]));

        var view = HabitsHeatmapBuilder.Build(done, kind: null, Today);

        var todayCell = view.Cells.Single(c => c.Weekday == 4 && c.Week == HabitsHeatmapBuilder.Weeks - 1);
        Assert.Equal(HabitsHeatmapBuilder.Bucket(3, 5), todayCell.Bucket);
    }

    [Fact]
    public void Build_OmitsFutureDaysOfCurrentWeek()
    {
        var view = HabitsHeatmapBuilder.Build(Done(), HabitKind.Strength, Today);

        // Heute ist Freitag (Weekday 4) → Sa/So der laufenden Woche fehlen.
        var currentWeek = HabitsHeatmapBuilder.Weeks - 1;
        Assert.DoesNotContain(view.Cells, c => c.Week == currentWeek && c.Weekday > 4);
        Assert.Contains(view.Cells, c => c.Week == currentWeek && c.Weekday == 4);
    }

    [Fact]
    public void Build_ComputesYearTotalCurrentWeekAndBars()
    {
        var monday = new DateOnly(2026, 6, 8); // Mo dieser Woche
        var done = Done((HabitKind.Strength,
        [
            monday, monday.AddDays(2), Today,   // 3 in dieser Woche
            new DateOnly(2026, 1, 5),           // früher im Jahr
            new DateOnly(2025, 12, 30),         // Vorjahr → zählt nicht ins Jahr
        ]));

        var view = HabitsHeatmapBuilder.Build(done, HabitKind.Strength, Today);

        Assert.Equal(4, view.YearTotal);          // 3 + 1, ohne Vorjahr
        Assert.Equal(3, view.CurrentWeek);
        Assert.Equal(12, view.WeeklyBars.Count);
        Assert.Equal(3, view.WeeklyBars[^1].Count); // jüngste Woche
        Assert.Equal(100, view.WeeklyBars[^1].BarPercent, 1);
    }
}
