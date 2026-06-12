using Dashboard.Domain.Time;
using Dashboard.Domain.Whoop;
using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Whoop;

/// <summary>
/// DB-Persistenz der WHOOP-Workouts (Tabelle <c>WhoopWorkouts</c>). Upsert ersetzt den
/// Datensatz vollständig anhand der UUID — Workout-Scores werden von WHOOP nachträglich
/// finalisiert, der jüngste Stand gewinnt.
/// </summary>
public sealed class WhoopWorkoutStore : IWhoopWorkoutStore
{
    private readonly IDbContextFactory<DashboardDbContext> _factory;
    private readonly IClock _clock;

    public WhoopWorkoutStore(IDbContextFactory<DashboardDbContext> factory, IClock clock)
    {
        _factory = factory;
        _clock = clock;
    }

    public async Task UpsertAsync(IReadOnlyList<WhoopWorkout> workouts, CancellationToken ct = default)
    {
        if (workouts.Count == 0)
        {
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(ct);
        var ids = workouts.Select(w => w.Id).ToList();
        var existing = await db.Set<WhoopWorkoutEntity>()
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        var now = _clock.UtcNow;
        foreach (var workout in workouts)
        {
            if (!existing.TryGetValue(workout.Id, out var entity))
            {
                entity = new WhoopWorkoutEntity { Id = workout.Id };
                db.Add(entity);
            }
            Apply(workout, entity, now);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<WhoopWorkout>> GetRangeAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entities = await db.Set<WhoopWorkoutEntity>().AsNoTracking()
            .Where(e => e.StartUtc >= fromUtc && e.StartUtc <= toUtc)
            .OrderBy(e => e.StartUtc)
            .ToListAsync(ct);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<DateTimeOffset?> GetOldestStartUtcAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Set<WhoopWorkoutEntity>().AsNoTracking()
            .MinAsync(e => (DateTimeOffset?)e.StartUtc, ct);
    }

    private static void Apply(WhoopWorkout workout, WhoopWorkoutEntity entity, DateTimeOffset nowUtc)
    {
        entity.Sport = workout.Sport;
        entity.StartUtc = workout.StartUtc;
        entity.EndUtc = workout.EndUtc;
        entity.DistanceMeters = workout.DistanceMeters;
        entity.HighIntensityShare = workout.HighIntensityShare;
        entity.Strain = workout.Strain;
        entity.Kilojoule = workout.Kilojoule;
        entity.AverageHeartRate = workout.AverageHeartRate;
        entity.MaxHeartRate = workout.MaxHeartRate;
        entity.Zone0Milli = workout.Zones?.Zone0Milli;
        entity.Zone1Milli = workout.Zones?.Zone1Milli;
        entity.Zone2Milli = workout.Zones?.Zone2Milli;
        entity.Zone3Milli = workout.Zones?.Zone3Milli;
        entity.Zone4Milli = workout.Zones?.Zone4Milli;
        entity.Zone5Milli = workout.Zones?.Zone5Milli;
        entity.UpdatedAtUtc = nowUtc;
    }

    private static WhoopWorkout ToDomain(WhoopWorkoutEntity e) => new(
        e.Id, e.Sport, e.StartUtc, e.EndUtc, e.DistanceMeters, e.HighIntensityShare,
        Strain: e.Strain,
        Kilojoule: e.Kilojoule,
        AverageHeartRate: e.AverageHeartRate,
        MaxHeartRate: e.MaxHeartRate,
        Zones: e is { Zone0Milli: { } z0, Zone1Milli: { } z1, Zone2Milli: { } z2, Zone3Milli: { } z3, Zone4Milli: { } z4, Zone5Milli: { } z5 }
            ? new WhoopZoneTimes(z0, z1, z2, z3, z4, z5)
            : null);
}
