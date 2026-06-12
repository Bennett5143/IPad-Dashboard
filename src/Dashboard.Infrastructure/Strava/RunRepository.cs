using Dashboard.Domain.Running;
using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Dashboard.Infrastructure.Strava;

/// <summary>
/// EF/PostGIS-Implementierung von <see cref="IRunRepository"/>. Bildet zwischen der Domänen-<see cref="Run"/>
/// (GeoPoint-Track) und der Persistenz-Entity (<see cref="LineString"/>) hin und her.
/// </summary>
public sealed class RunRepository : IRunRepository
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private readonly IDbContextFactory<DashboardDbContext> _factory;

    public RunRepository(IDbContextFactory<DashboardDbContext> factory) => _factory = factory;

    public async Task UpsertAsync(IReadOnlyList<Run> runs, CancellationToken ct = default)
    {
        if (runs.Count == 0)
        {
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(ct);
        var ids = runs.Select(r => r.Id).ToList();
        var existing = await db.Set<RunActivityEntity>()
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        foreach (var run in runs)
        {
            if (existing.TryGetValue(run.Id, out var entity))
            {
                Apply(run, entity);
            }
            else
            {
                var created = new RunActivityEntity { Id = run.Id };
                Apply(run, created);
                db.Add(created);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Run>> GetRunsAsync(DateTimeOffset? sinceUtc, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var query = db.Set<RunActivityEntity>().AsNoTracking();
        if (sinceUtc is { } since)
        {
            query = query.Where(e => e.StartUtc >= since);
        }

        var entities = await query.OrderByDescending(e => e.StartUtc).ToListAsync(ct);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Run>> GetRunSummariesAsync(
        DateTimeOffset? sinceUtc, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var query = db.Set<RunActivityEntity>().AsNoTracking();
        if (sinceUtc is { } since)
        {
            query = query.Where(e => e.StartUtc >= since);
        }

        // Bewusst eine Projektion ohne Route: der LineString ist für Metrik-Auswertungen
        // unnötig schwer (volle Geometrie pro Lauf).
        var rows = await query
            .OrderByDescending(e => e.StartUtc)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.Type,
                e.StartUtc,
                e.DistanceMeters,
                e.MovingTimeSeconds,
                e.ElevationGainMeters,
                e.AverageHeartRate,
                e.MaxHeartRate
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new Run(
                r.Id, r.Name, r.Type, r.StartUtc, r.DistanceMeters,
                TimeSpan.FromSeconds(r.MovingTimeSeconds), [],
                ElevationGainMeters: r.ElevationGainMeters,
                AverageHeartRate: r.AverageHeartRate,
                MaxHeartRate: r.MaxHeartRate))
            .ToList();
    }

    public async Task<DateTimeOffset?> GetLatestRunStartAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        if (!await db.Set<RunActivityEntity>().AnyAsync(ct))
        {
            return null;
        }

        return await db.Set<RunActivityEntity>().MaxAsync(e => e.StartUtc, ct);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Set<RunActivityEntity>().CountAsync(ct);
    }

    public async Task<IReadOnlyList<long>> GetIdsMissingStreamsAsync(int limit, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Set<RunActivityEntity>().AsNoTracking()
            .Where(e => !e.StreamsFetched)
            .OrderByDescending(e => e.StartUtc)
            .Take(limit)
            .Select(e => e.Id)
            .ToListAsync(ct);
    }

    public async Task SaveStreamsAsync(long runId, StravaStreams? streams, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.Set<RunActivityEntity>().FirstOrDefaultAsync(e => e.Id == runId, ct);
        if (entity is null)
        {
            return;
        }

        entity.StreamsFetched = true;
        if (streams is not null && streams.Track.Count >= 2)
        {
            // Volle Auflösung aus dem latlng-Stream übernehmen + index-aligned Streams.
            entity.Route = ToLineString(streams.Track);
            entity.TimeOffsetsSeconds = [.. streams.TimeOffsetsSeconds];
            entity.AltitudesMeters = streams.AltitudesMeters is null ? null : [.. streams.AltitudesMeters];
            entity.HeartRates = streams.HeartRates is null ? null : [.. streams.HeartRates];
        }

        await db.SaveChangesAsync(ct);
    }

    private static void Apply(Run run, RunActivityEntity entity)
    {
        entity.Name = run.Name;
        entity.Type = run.Type;
        entity.StartUtc = run.StartUtc;
        entity.DistanceMeters = run.DistanceMeters;
        entity.MovingTimeSeconds = (int)run.MovingTime.TotalSeconds;
        entity.ElevationGainMeters = run.ElevationGainMeters;
        entity.AverageHeartRate = run.AverageHeartRate;
        entity.MaxHeartRate = run.MaxHeartRate;

        // Solange noch keine vollaufgelösten Streams da sind, die Summary-Polyline verwenden;
        // nach dem Stream-Backfill die feinere Route nicht durch die grobe Polyline überschreiben.
        if (!entity.StreamsFetched)
        {
            entity.Route = ToLineString(run.Track);
        }
    }

    private static LineString? ToLineString(IReadOnlyList<GeoPoint> track)
    {
        if (track.Count < 2)
        {
            return null; // ein LineString braucht mind. zwei Punkte
        }

        var coordinates = track.Select(p => new Coordinate(p.Longitude, p.Latitude)).ToArray();
        return GeometryFactory.CreateLineString(coordinates);
    }

    private static Run ToDomain(RunActivityEntity entity)
    {
        IReadOnlyList<GeoPoint> track = entity.Route is null
            ? []
            : entity.Route.Coordinates.Select(c => new GeoPoint(c.Y, c.X)).ToList();

        StravaStreams? streams = null;
        if (entity.StreamsFetched && entity.TimeOffsetsSeconds is { } times && track.Count >= 2)
        {
            streams = new StravaStreams(track, times, entity.AltitudesMeters, entity.HeartRates);
        }

        return new Run(
            entity.Id, entity.Name, entity.Type, entity.StartUtc,
            entity.DistanceMeters, TimeSpan.FromSeconds(entity.MovingTimeSeconds), track, streams,
            ElevationGainMeters: entity.ElevationGainMeters,
            AverageHeartRate: entity.AverageHeartRate,
            MaxHeartRate: entity.MaxHeartRate);
    }
}
