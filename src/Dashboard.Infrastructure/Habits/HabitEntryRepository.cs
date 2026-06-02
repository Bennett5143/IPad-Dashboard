using Dashboard.Domain.Entities;              
using Dashboard.Domain.Habits;                 
using Dashboard.Infrastructure.Persistence;    
using Microsoft.EntityFrameworkCore;           
using Dashboard.Domain.Enums;

namespace Dashboard.Infrastructure.Habits;

public sealed class HabitEntryRepository : IHabitEntryRepository
{
    private readonly IDbContextFactory<DashboardDbContext> _factory;

    public HabitEntryRepository(IDbContextFactory<DashboardDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<HabitEntry?> GetAsync(DateOnly date, HabitKind kind, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.HabitEntries
            .FirstOrDefaultAsync(e => e.Date == date && e.Kind == kind, ct);
    }

    public async Task<IReadOnlySet<HabitKind>> GetCompletedKindsAsync(
        DateOnly date, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var kinds = await db.HabitEntries
            .Where(e => e.Date == date)
            .Select(e => e.Kind)
            .ToListAsync(ct);
        return kinds.ToHashSet();
    }

    public async Task<IReadOnlyDictionary<HabitKind, int>> CountByKindAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.HabitEntries
            .Where(e => e.Date >= from && e.Date <= to)
            .GroupBy(e => e.Kind)
            .Select(g => new { Kind = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Kind, x => x.Count, ct);
    }

    public async Task AddAsync(HabitEntry entry, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.HabitEntries.Add(entry);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(HabitEntry entry, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.HabitEntries.Attach(entry);
        db.HabitEntries.Remove(entry);
        await db.SaveChangesAsync(ct);
    }

    public async Task<EmomWorkout?> GetEmomAsync(DateOnly date, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entry = await db.HabitEntries
            .Include(e => e.Emom!).ThenInclude(w => w.Segments)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Date == date && e.Kind == HabitKind.Strength, ct);
        return entry?.Emom;
    }

    public async Task UpsertEmomAsync(
        DateOnly date, IReadOnlyList<EmomSegment> segments, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entry = await db.HabitEntries
            .Include(e => e.Emom!).ThenInclude(w => w.Segments)
            .FirstOrDefaultAsync(e => e.Date == date && e.Kind == HabitKind.Strength, ct);

        if (entry is null)
        {
            entry = new HabitEntry { Date = date, Kind = HabitKind.Strength };
            db.HabitEntries.Add(entry);
        }
        else if (entry.Emom is not null)
        {
            db.Remove(entry.Emom);   // alte Segmente per Cascade weg
        }

        entry.Emom = new EmomWorkout
        {
            Segments = segments.Select(s => new EmomSegment
            {
                FromMinute = s.FromMinute,
                ToMinute = s.ToMinute,
                PushupsPerMinute = s.PushupsPerMinute,
                PullupsPerMinute = s.PullupsPerMinute
            }).ToList()
        };

        await db.SaveChangesAsync(ct);
    }
}