using Dashboard.Domain.Entities; 
using Dashboard.Domain.Enums;

namespace Dashboard.Domain.Habits;

public interface IHabitEntryRepository
{
    Task<HabitEntry?> GetAsync(DateOnly date, HabitKind kind, CancellationToken ct = default);
    Task<IReadOnlySet<HabitKind>> GetCompletedKindsAsync(DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyDictionary<HabitKind, int>> CountByKindAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default);
    Task AddAsync(HabitEntry entry, CancellationToken ct = default);
    Task RemoveAsync(HabitEntry entry, CancellationToken ct = default);
    Task<EmomWorkout?> GetEmomAsync(DateOnly date, CancellationToken ct = default);
    Task UpsertEmomAsync(DateOnly date, IReadOnlyList<EmomSegment> segments, CancellationToken ct = default);
}