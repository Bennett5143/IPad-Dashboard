using Dashboard.Domain.Entities;   
using Dashboard.Domain.Enums;


namespace Dashboard.Domain.Habits;

public sealed record HabitSummary(
    HabitKind Kind,
    bool IsDoneToday,
    int WeekCount,
    int YearCount,
    EmomWorkout? TodaysEmom = null);