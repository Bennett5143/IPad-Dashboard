using Dashboard.Domain.Whoop;
using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Whoop;

/// <summary>DB-Persistenz der bereits verarbeiteten WHOOP-Workout-IDs.</summary>
public sealed class WhoopProcessedWorkoutStore : IWhoopProcessedWorkoutStore
{
    private readonly IDbContextFactory<DashboardDbContext> _factory;

    public WhoopProcessedWorkoutStore(IDbContextFactory<DashboardDbContext> factory) => _factory = factory;

    public async Task<IReadOnlySet<string>> GetProcessedAsync(
        IReadOnlyCollection<string> workoutIds, CancellationToken ct = default)
    {
        if (workoutIds.Count == 0)
        {
            return new HashSet<string>();
        }

        await using var db = await _factory.CreateDbContextAsync(ct);
        var ids = await db.Set<WhoopProcessedWorkoutEntity>().AsNoTracking()
            .Where(e => workoutIds.Contains(e.WorkoutId))
            .Select(e => e.WorkoutId)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    public async Task MarkProcessedAsync(string workoutId, DateTimeOffset whenUtc, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        if (await db.Set<WhoopProcessedWorkoutEntity>().AnyAsync(e => e.WorkoutId == workoutId, ct))
        {
            return;
        }

        db.Add(new WhoopProcessedWorkoutEntity { WorkoutId = workoutId, ProcessedAtUtc = whenUtc });
        await db.SaveChangesAsync(ct);
    }
}
