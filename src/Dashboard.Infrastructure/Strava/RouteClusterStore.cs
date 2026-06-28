using Dashboard.Domain.Running;
using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Strava;

/// <summary>
/// EF/PostGIS-Persistenz der Routen-Cluster (FA-8.17). Der Vergleich selbst läuft in der
/// Domäne (<see cref="RouteClusterer"/>) über die hier geladenen Tracks.
/// </summary>
public sealed class RouteClusterStore : IRouteClusterStore
{
    private readonly IDbContextFactory<DashboardDbContext> _factory;

    public RouteClusterStore(IDbContextFactory<DashboardDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<RouteClusterRepresentative>> GetRepresentativesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var clusters = await db.Set<RouteClusterEntity>().AsNoTracking().ToListAsync(ct);
        if (clusters.Count == 0)
        {
            return [];
        }

        var repIds = clusters.Select(c => c.RepresentativeRunId).ToList();
        var routes = await db.Set<RunActivityEntity>().AsNoTracking()
            .Where(e => repIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Route })
            .ToListAsync(ct);
        var trackById = routes.ToDictionary(r => r.Id, r => ToTrack(r.Route));

        return clusters
            .Where(c => trackById.TryGetValue(c.RepresentativeRunId, out var track) && track.Count >= 2)
            .Select(c => new RouteClusterRepresentative(
                c.Id, c.RepresentativeDistanceMeters, trackById[c.RepresentativeRunId]))
            .ToList();
    }

    public async Task<IReadOnlyList<long>> GetUnmatchedRunIdsAsync(int limit, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Set<RunActivityEntity>().AsNoTracking()
            .Where(e => e.RouteMatchedUtc == null && e.Route != null)
            .OrderBy(e => e.StartUtc) // chronologisch → deterministische Repräsentanten
            .Take(limit)
            .Select(e => e.Id)
            .ToListAsync(ct);
    }

    public async Task<int> CreateClusterAsync(
        long representativeRunId, double distanceMeters, DateTimeOffset whenUtc, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var number = await db.Set<RouteClusterEntity>().CountAsync(ct) + 1;

        var cluster = new RouteClusterEntity
        {
            Name = $"Runde {number}",
            RepresentativeRunId = representativeRunId,
            RepresentativeDistanceMeters = distanceMeters,
            CreatedUtc = whenUtc
        };
        db.Add(cluster);
        await db.SaveChangesAsync(ct); // Id generieren

        await MarkAsync(db, representativeRunId, cluster.Id, whenUtc, ct);
        return cluster.Id;
    }

    public async Task AssignAsync(long runId, int clusterId, DateTimeOffset whenUtc, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await MarkAsync(db, runId, clusterId, whenUtc, ct);
    }

    public async Task MarkUnclusterableAsync(long runId, DateTimeOffset whenUtc, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await MarkAsync(db, runId, clusterId: null, whenUtc, ct);
    }

    public async Task<IReadOnlyList<RouteClusterSummary>> GetSummariesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var names = await db.Set<RouteClusterEntity>().AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var grouped = await db.Set<RunActivityEntity>().AsNoTracking()
            .Where(e => e.RouteClusterId != null)
            .GroupBy(e => e.RouteClusterId!.Value)
            .Select(g => new
            {
                ClusterId = g.Key,
                MemberCount = g.Count(),
                TotalMeters = g.Sum(e => e.DistanceMeters),
                TotalSeconds = g.Sum(e => (long)e.MovingTimeSeconds),
                BestSeconds = g.Min(e => e.MovingTimeSeconds)
            })
            .ToListAsync(ct);

        return grouped
            .Select(g =>
            {
                var totalKm = g.TotalMeters / 1000.0;
                return new RouteClusterSummary(
                    g.ClusterId,
                    names.GetValueOrDefault(g.ClusterId, $"Runde {g.ClusterId}"),
                    g.MemberCount,
                    totalKm / g.MemberCount,
                    totalKm > 0 ? g.TotalSeconds / 60.0 / totalKm : null,
                    TimeSpan.FromSeconds(g.BestSeconds));
            })
            .OrderByDescending(s => s.MemberCount)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<RouteClusterInfo?> GetClusterForRunAsync(long runId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var clusterId = await db.Set<RunActivityEntity>().AsNoTracking()
            .Where(e => e.Id == runId)
            .Select(e => e.RouteClusterId)
            .FirstOrDefaultAsync(ct);
        if (clusterId is null)
        {
            return null;
        }

        var cluster = await db.Set<RouteClusterEntity>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clusterId, ct);
        return cluster is null ? null : new RouteClusterInfo(cluster.Id, cluster.Name);
    }

    public async Task<IReadOnlyList<long>> GetRunIdsForClusterAsync(int clusterId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Set<RunActivityEntity>().AsNoTracking()
            .Where(e => e.RouteClusterId == clusterId)
            .OrderBy(e => e.StartUtc)
            .Select(e => e.Id)
            .ToListAsync(ct);
    }

    private static async Task MarkAsync(
        DashboardDbContext db, long runId, int? clusterId, DateTimeOffset whenUtc, CancellationToken ct)
    {
        var run = await db.Set<RunActivityEntity>().FirstOrDefaultAsync(e => e.Id == runId, ct);
        if (run is null)
        {
            return;
        }

        run.RouteClusterId = clusterId;
        run.RouteMatchedUtc = whenUtc;
        await db.SaveChangesAsync(ct);
    }

    private static IReadOnlyList<GeoPoint> ToTrack(NetTopologySuite.Geometries.LineString? route) =>
        route is null ? [] : route.Coordinates.Select(c => new GeoPoint(c.Y, c.X)).ToList();
}
