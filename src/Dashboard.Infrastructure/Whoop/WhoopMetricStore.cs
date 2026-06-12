using Dashboard.Domain.Time;
using Dashboard.Domain.Whoop;
using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Whoop;

/// <summary>
/// DB-Persistenz der WHOOP-Tageswerte (Tabelle <c>WhoopDailyMetrics</c>). Upsert arbeitet
/// feldweise: Neue Werte überschreiben (WHOOP re-scored nachträglich), <c>null</c> lässt
/// Bestehendes stehen — ein Fenster, das einen Tag nur anschneidet, darf nichts löschen.
/// </summary>
public sealed class WhoopMetricStore : IWhoopMetricStore
{
    private readonly IDbContextFactory<DashboardDbContext> _factory;
    private readonly IClock _clock;

    public WhoopMetricStore(IDbContextFactory<DashboardDbContext> factory, IClock clock)
    {
        _factory = factory;
        _clock = clock;
    }

    public async Task UpsertAsync(IReadOnlyList<WhoopDailyMetric> metrics, CancellationToken ct = default)
    {
        if (metrics.Count == 0)
        {
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(ct);
        var dates = metrics.Select(m => m.Date).ToList();
        var existing = await db.Set<WhoopDailyMetricEntity>()
            .Where(e => dates.Contains(e.Date))
            .ToDictionaryAsync(e => e.Date, ct);

        var now = _clock.UtcNow;
        foreach (var metric in metrics)
        {
            if (!existing.TryGetValue(metric.Date, out var entity))
            {
                entity = new WhoopDailyMetricEntity { Date = metric.Date };
                db.Add(entity);
            }
            Apply(metric, entity, now);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<WhoopDailyMetric>> GetRangeAsync(
        DateOnly fromInclusive, DateOnly toInclusive, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entities = await db.Set<WhoopDailyMetricEntity>().AsNoTracking()
            .Where(e => e.Date >= fromInclusive && e.Date <= toInclusive)
            .OrderBy(e => e.Date)
            .ToListAsync(ct);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<DateOnly?> GetOldestDateAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Set<WhoopDailyMetricEntity>().AsNoTracking()
            .MinAsync(e => (DateOnly?)e.Date, ct);
    }

    private static void Apply(WhoopDailyMetric metric, WhoopDailyMetricEntity entity, DateTimeOffset nowUtc)
    {
        entity.RecoveryScore = metric.RecoveryScore ?? entity.RecoveryScore;
        entity.HrvMillis = metric.HrvMillis ?? entity.HrvMillis;
        entity.RestingHeartRate = metric.RestingHeartRate ?? entity.RestingHeartRate;
        entity.SleepHours = metric.SleepHours ?? entity.SleepHours;
        entity.SleepPerformance = metric.SleepPerformance ?? entity.SleepPerformance;
        entity.DayStrain = metric.DayStrain ?? entity.DayStrain;
        entity.LightSleepHours = metric.LightSleepHours ?? entity.LightSleepHours;
        entity.DeepSleepHours = metric.DeepSleepHours ?? entity.DeepSleepHours;
        entity.RemSleepHours = metric.RemSleepHours ?? entity.RemSleepHours;
        entity.AwakeHours = metric.AwakeHours ?? entity.AwakeHours;
        entity.SleepStartUtc = metric.SleepStartUtc ?? entity.SleepStartUtc;
        entity.SleepEndUtc = metric.SleepEndUtc ?? entity.SleepEndUtc;
        entity.RespiratoryRate = metric.RespiratoryRate ?? entity.RespiratoryRate;
        entity.Spo2Percentage = metric.Spo2Percentage ?? entity.Spo2Percentage;
        entity.SkinTempCelsius = metric.SkinTempCelsius ?? entity.SkinTempCelsius;
        entity.UpdatedAtUtc = nowUtc;
    }

    private static WhoopDailyMetric ToDomain(WhoopDailyMetricEntity e) => new(
        e.Date, e.RecoveryScore, e.HrvMillis, e.RestingHeartRate,
        e.SleepHours, e.SleepPerformance, e.DayStrain,
        LightSleepHours: e.LightSleepHours,
        DeepSleepHours: e.DeepSleepHours,
        RemSleepHours: e.RemSleepHours,
        AwakeHours: e.AwakeHours,
        SleepStartUtc: e.SleepStartUtc,
        SleepEndUtc: e.SleepEndUtc,
        RespiratoryRate: e.RespiratoryRate,
        Spo2Percentage: e.Spo2Percentage,
        SkinTempCelsius: e.SkinTempCelsius);
}
