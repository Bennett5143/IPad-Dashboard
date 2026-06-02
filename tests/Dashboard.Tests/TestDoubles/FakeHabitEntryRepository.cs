namespace Dashboard.Tests.TestDoubles;

internal sealed class FakeHabitEntryRepository : IHabitEntryRepository
{
    private readonly List<HabitEntry> _entries = new();
    private int _nextId = 1;

    public Task<HabitEntry?> GetAsync(DateOnly date, HabitKind kind, CancellationToken ct = default)
        => Task.FromResult(_entries.FirstOrDefault(e => e.Date == date && e.Kind == kind));

    public Task<IReadOnlySet<HabitKind>> GetCompletedKindsAsync(DateOnly date, CancellationToken ct = default)
        => Task.FromResult<IReadOnlySet<HabitKind>>(
            _entries.Where(e => e.Date == date).Select(e => e.Kind).ToHashSet());

    public Task<IReadOnlyDictionary<HabitKind, int>> CountByKindAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<HabitKind, int>>(
            _entries
                .Where(e => e.Date >= from && e.Date <= to)
                .GroupBy(e => e.Kind)
                .ToDictionary(g => g.Key, g => g.Count()));

    public Task AddAsync(HabitEntry entry, CancellationToken ct = default)
    {
        if (_entries.Any(e => e.Date == entry.Date && e.Kind == entry.Kind))
            throw new InvalidOperationException("Unique constraint violation in fake.");
        entry.Id = _nextId++;
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(HabitEntry entry, CancellationToken ct = default)
    {
        _entries.RemoveAll(e => e.Id == entry.Id);
        return Task.CompletedTask;
    }

    private readonly Dictionary<DateOnly, EmomWorkout> _emoms = new();

    public Task<EmomWorkout?> GetEmomAsync(DateOnly date, CancellationToken ct = default)
        => Task.FromResult(_emoms.TryGetValue(date, out var w) ? w : null);

    public Task UpsertEmomAsync(
        DateOnly date, IReadOnlyList<EmomSegment> segments, CancellationToken ct = default)
    {
        _emoms[date] = new EmomWorkout { Segments = segments.ToList() };
        if (!_entries.Any(e => e.Date == date && e.Kind == HabitKind.Strength))
            _entries.Add(new HabitEntry { Id = _nextId++, Date = date, Kind = HabitKind.Strength });
        return Task.CompletedTask;
    }
}