using Dashboard.Domain.Entities;
using Dashboard.Domain.Enums;
using Dashboard.Domain.ValueObjects;

namespace Dashboard.Domain.Habits;

public interface IHabitEntryRepository
{
    Task<HabitEntry?> GetAsync(DateOnly date, HabitKind kind, CancellationToken ct = default);
    Task<IReadOnlySet<HabitKind>> GetCompletedKindsAsync(DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyDictionary<HabitKind, int>> CountByKindAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// Erledigte Tage je Habit im Bereich (beide Grenzen inklusiv) – Basis für Verlaufs-Heatmap
    /// und Streaks (FA-3.08/3.09). Habits ohne Eintrag fehlen im Dictionary.
    /// </summary>
    Task<IReadOnlyDictionary<HabitKind, IReadOnlySet<DateOnly>>> GetEntryDatesAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default);
    Task AddAsync(HabitEntry entry, CancellationToken ct = default);
    Task RemoveAsync(HabitEntry entry, CancellationToken ct = default);
    Task<EmomWorkout?> GetEmomAsync(DateOnly date, CancellationToken ct = default);
    Task UpsertEmomAsync(DateOnly date, IReadOnlyList<EmomSegment> segments, CancellationToken ct = default);
    Task<IReadOnlyDictionary<HabitKind, RunningDetails>> GetRunningForDateAsync(
        DateOnly date, CancellationToken ct = default);
    Task UpsertRunningAsync(
        DateOnly date, HabitKind kind, RunningDetails details, CancellationToken ct = default);
}
