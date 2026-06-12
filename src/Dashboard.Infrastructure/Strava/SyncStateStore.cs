using Dashboard.Domain.Running;
using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Strava;

/// <summary>Single-Row-Persistenz des Sync-Zustands (letzter Erfolg/Versuch/Fehler).</summary>
public sealed class SyncStateStore : ISyncStateStore
{
    private const int RowId = 1;

    private readonly IDbContextFactory<DashboardDbContext> _factory;

    public SyncStateStore(IDbContextFactory<DashboardDbContext> factory) => _factory = factory;

    public async Task<SyncSnapshot> GetAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.Set<SyncStateEntity>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == RowId, ct);

        return entity is null
            ? new SyncSnapshot(null, null, null)
            : new SyncSnapshot(
                entity.LastSuccessfulSyncUtc, entity.LastAttemptUtc, entity.LastError,
                entity.DetailsBackfilledUtc);
    }

    public Task RecordSuccessAsync(DateTimeOffset whenUtc, CancellationToken ct = default) =>
        UpdateAsync(e =>
        {
            e.LastSuccessfulSyncUtc = whenUtc;
            e.LastAttemptUtc = whenUtc;
            e.LastError = null;
        }, ct);

    public Task RecordFailureAsync(string error, DateTimeOffset whenUtc, CancellationToken ct = default) =>
        UpdateAsync(e =>
        {
            e.LastAttemptUtc = whenUtc;
            e.LastError = error;
        }, ct);

    public Task MarkDetailsBackfilledAsync(DateTimeOffset whenUtc, CancellationToken ct = default) =>
        UpdateAsync(e => e.DetailsBackfilledUtc = whenUtc, ct);

    private async Task UpdateAsync(Action<SyncStateEntity> mutate, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.Set<SyncStateEntity>().FirstOrDefaultAsync(e => e.Id == RowId, ct);

        if (entity is null)
        {
            entity = new SyncStateEntity { Id = RowId };
            mutate(entity);
            db.Add(entity);
        }
        else
        {
            mutate(entity);
        }

        await db.SaveChangesAsync(ct);
    }
}
