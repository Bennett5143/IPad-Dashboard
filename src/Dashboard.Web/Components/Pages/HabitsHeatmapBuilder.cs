using System.Globalization;

using Dashboard.Domain.Habits;

namespace Dashboard.Web.Components.Pages;

/// <summary>Eine Zelle der Jahres-Heatmap (Position im Grid + Intensitäts-Bucket).</summary>
public sealed record HabitHeatCell(int Week, int Weekday, int Bucket, string Title);

/// <summary>Ein Wochenbalken (Anzahl Erledigungen je Kalenderwoche).</summary>
public sealed record HabitWeekBar(string Label, int Count, double BarPercent);

/// <summary>Monatsbeschriftung über der Heatmap (Spaltenindex der ersten Woche des Monats).</summary>
public sealed record HabitMonthLabel(int Week, string Label);

/// <summary>View-Modell der `/habits`-Seite für die gewählte Auswahl (Alle oder ein Habit).</summary>
public sealed record HabitHeatmapView(
    IReadOnlyList<HabitHeatCell> Cells,
    IReadOnlyList<HabitMonthLabel> Months,
    int MaxBucket,
    int YearTotal,
    int CurrentWeek,
    IReadOnlyList<HabitWeekBar> WeeklyBars);

/// <summary>
/// Baut die Habit-Verlaufs-Heatmap (FA-3.08) — GitHub-Style-Jahresgrid (Wochen × Wochentage,
/// Mo oben) plus Wochenbalken. Reine, testbare Logik; alle Tage in der lokalen Datums-Sicht
/// des aufrufenden Clocks (Berlin).
/// </summary>
public static class HabitsHeatmapBuilder
{
    public const int Weeks = 53;
    private const int RecentWeekBars = 12;

    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly int KindCount = Enum.GetValues<HabitKind>().Length;

    public static string Label(HabitKind kind) => kind switch
    {
        HabitKind.Strength => "Gym",
        HabitKind.Zone2Run => "Z2-Lauf",
        HabitKind.Vo2MaxIntervals => "VO2 Max",
        HabitKind.JumpRope => "Seilspringen",
        HabitKind.Stretching => "Dehnen",
        _ => kind.ToString()
    };

    /// <param name="kind">Ein Habit, oder <c>null</c> für „Alle" (Intensität = Anzahl Habits je Tag).</param>
    public static HabitHeatmapView Build(
        IReadOnlyDictionary<HabitKind, IReadOnlySet<DateOnly>> doneByKind, HabitKind? kind, DateOnly today)
    {
        var thisMonday = HabitWeek.ContainingDate(today).Start;
        var start = thisMonday.AddDays(-(Weeks - 1) * 7);
        var maxLevel = kind is null ? KindCount : 1;

        int Level(DateOnly d) => kind is { } k
            ? (doneByKind.TryGetValue(k, out var set) && set.Contains(d) ? 1 : 0)
            : doneByKind.Count(kv => kv.Value.Contains(d));

        int CountInRange(DateOnly from, DateOnly to) => kind is { } k
            ? (doneByKind.TryGetValue(k, out var set) ? set.Count(d => d >= from && d <= to) : 0)
            : doneByKind.Sum(kv => kv.Value.Count(d => d >= from && d <= to));

        var cells = new List<HabitHeatCell>();
        var months = new List<HabitMonthLabel>();
        var lastMonth = 0;
        for (var w = 0; w < Weeks; w++)
        {
            var weekStart = start.AddDays(w * 7);
            if (weekStart.Month != lastMonth && weekStart.Day <= 7)
            {
                months.Add(new HabitMonthLabel(w, weekStart.ToString("MMM", German)));
                lastMonth = weekStart.Month;
            }

            for (var wd = 0; wd < 7; wd++)
            {
                var date = weekStart.AddDays(wd);
                if (date > today)
                {
                    continue; // künftige Tage der laufenden Woche bleiben leer
                }

                var level = Level(date);
                cells.Add(new HabitHeatCell(w, wd, Bucket(level, maxLevel), CellTitle(date, level, kind)));
            }
        }

        var bars = new List<HabitWeekBar>();
        for (var i = RecentWeekBars - 1; i >= 0; i--)
        {
            var monday = thisMonday.AddDays(-i * 7);
            bars.Add(new HabitWeekBar(monday.ToString("dd.MM.", German), CountInRange(monday, monday.AddDays(6)), 0));
        }
        var maxBar = Math.Max(1, bars.Max(b => b.Count));
        bars = bars.Select(b => b with { BarPercent = (double)b.Count / maxBar * 100 }).ToList();

        return new HabitHeatmapView(
            cells,
            months,
            4,
            CountInRange(new DateOnly(today.Year, 1, 1), today),
            CountInRange(thisMonday, today),
            bars);
    }

    /// <summary>Intensität auf 0..4 abbilden (0 = nichts, 4 = voll).</summary>
    public static int Bucket(int level, int maxLevel)
    {
        if (level <= 0)
        {
            return 0;
        }
        if (maxLevel <= 1)
        {
            return 4;
        }

        return Math.Clamp(1 + (int)Math.Round((level - 1) / (double)(maxLevel - 1) * 3), 1, 4);
    }

    private static string CellTitle(DateOnly date, int level, HabitKind? kind)
    {
        var day = date.ToString("dd.MM.yyyy", German);
        if (kind is { } k)
        {
            return level > 0 ? $"{day}: {Label(k)} erledigt" : $"{day}: {Label(k)} offen";
        }

        return level switch
        {
            0 => $"{day}: kein Habit",
            1 => $"{day}: 1 Habit",
            _ => $"{day}: {level} Habits"
        };
    }
}
