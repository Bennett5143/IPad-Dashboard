using Dashboard.Domain.Running;
using Dashboard.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Strava;

/// <summary>
/// EF-Persistenz der Lauf-Orte. Der Vergleich selbst liegt in der Domäne
/// (<see cref="RunPlaceMatcher"/>); hier werden Mittelpunkt und Ausdehnung fortgeschrieben und
/// die Aggregate für die Übersicht gebildet.
/// </summary>
public sealed class RunPlaceStore : IRunPlaceStore
{
    private readonly IDbContextFactory<DashboardDbContext> _factory;

    public RunPlaceStore(IDbContextFactory<DashboardDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<RunPlaceCandidate>> GetCandidatesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Set<RunPlaceEntity>().AsNoTracking()
            .Select(place => new RunPlaceCandidate(
                place.Id, new GeoPoint(place.CentreLatitude, place.CentreLongitude)))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<long>> GetUnassignedRunIdsAsync(int limit, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Set<RunActivityEntity>().AsNoTracking()
            .Where(run => run.PlaceAssignedUtc == null && run.Route != null)
            .OrderBy(run => run.StartUtc) // chronologisch → deterministische Orts-Reihenfolge
            .Take(limit)
            .Select(run => run.Id)
            .ToListAsync(ct);
    }

    public async Task<int> CreatePlaceAsync(
        long runId, IReadOnlyList<GeoPoint> track, DateTimeOffset whenUtc, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var start = track[0];
        var bounds = GeoBounds.Around(start).ExtendAll(track);
        var number = await db.Set<RunPlaceEntity>().CountAsync(ct) + 1;

        var place = new RunPlaceEntity
        {
            // Vorläufiger Name, bis er in der Übersicht vergeben wird.
            Name = $"Ort {number}",
            CentreLatitude = start.Latitude,
            CentreLongitude = start.Longitude,
            MinLatitude = bounds.MinLat,
            MinLongitude = bounds.MinLon,
            MaxLatitude = bounds.MaxLat,
            MaxLongitude = bounds.MaxLon,
            RunCount = 0,
            CreatedUtc = whenUtc,
        };

        db.Add(place);
        await db.SaveChangesAsync(ct); // Id erzeugen

        await AssignAsync(runId, place.Id, track, whenUtc, ct);
        return place.Id;
    }

    public async Task AssignAsync(
        long runId, int placeId, IReadOnlyList<GeoPoint> track, DateTimeOffset whenUtc,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var place = await db.Set<RunPlaceEntity>().FirstOrDefaultAsync(p => p.Id == placeId, ct);
        if (place is null)
        {
            return;
        }

        if (track.Count > 0)
        {
            var centre = RunPlaceMatcher.MoveCentre(
                new GeoPoint(place.CentreLatitude, place.CentreLongitude), place.RunCount, track[0]);
            var bounds = new GeoBounds(
                place.MinLatitude, place.MinLongitude, place.MaxLatitude, place.MaxLongitude)
                .ExtendAll(track);

            place.CentreLatitude = centre.Latitude;
            place.CentreLongitude = centre.Longitude;
            place.MinLatitude = bounds.MinLat;
            place.MinLongitude = bounds.MinLon;
            place.MaxLatitude = bounds.MaxLat;
            place.MaxLongitude = bounds.MaxLon;
        }

        place.RunCount++;

        var run = await db.Set<RunActivityEntity>().FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is not null)
        {
            run.PlaceId = placeId;
            run.PlaceAssignedUtc = whenUtc;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task MarkUnassignableAsync(long runId, DateTimeOffset whenUtc, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var run = await db.Set<RunActivityEntity>().FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
        {
            return;
        }

        run.PlaceId = null;
        run.PlaceAssignedUtc = whenUtc;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RunPlaceSummary>> GetSummariesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var rows = await db.Set<RunActivityEntity>().AsNoTracking()
            .Where(run => run.PlaceId != null)
            .GroupBy(run => run.PlaceId!.Value)
            .Select(group => new
            {
                PlaceId = group.Key,
                RunCount = group.Count(),
                TotalMeters = group.Sum(run => run.DistanceMeters),
                TotalSeconds = group.Sum(run => (double)run.MovingTimeSeconds),
                LastRunUtc = group.Max(run => (DateTimeOffset?)run.StartUtc),
            })
            .ToListAsync(ct);

        var names = await db.Set<RunPlaceEntity>().AsNoTracking()
            .ToDictionaryAsync(place => place.Id, place => place.Name, ct);

        return rows
            .Select(row => new RunPlaceSummary(
                row.PlaceId,
                names.GetValueOrDefault(row.PlaceId, $"Ort {row.PlaceId}"),
                row.RunCount,
                row.TotalMeters / 1000.0,
                // Ø-Pace über die Summen, nicht als Mittel der Einzel-Paces: sonst zählte ein
                // kurzer Sprint so viel wie ein langer Lauf.
                row.TotalMeters > 0 ? row.TotalSeconds / 60.0 / (row.TotalMeters / 1000.0) : null,
                row.LastRunUtc))
            .OrderByDescending(place => place.RunCount)
            .ThenBy(place => place.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    public async Task<IReadOnlyList<RunPlace>> GetPlacesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var places = await db.Set<RunPlaceEntity>().AsNoTracking()
            .OrderByDescending(place => place.RunCount)
            .ThenBy(place => place.Id)
            .ToListAsync(ct);

        return places
            .Select(place => new RunPlace(
                place.Id,
                place.Name,
                new GeoPoint(place.CentreLatitude, place.CentreLongitude),
                new GeoBounds(place.MinLatitude, place.MinLongitude, place.MaxLatitude, place.MaxLongitude),
                place.RunCount))
            .ToList();
    }

    public async Task<RunPlaceInfo?> GetPlaceForRunAsync(long runId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var placeId = await db.Set<RunActivityEntity>().AsNoTracking()
            .Where(run => run.Id == runId)
            .Select(run => run.PlaceId)
            .FirstOrDefaultAsync(ct);

        if (placeId is not { } id)
        {
            return null;
        }

        var place = await db.Set<RunPlaceEntity>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return place is null ? null : new RunPlaceInfo(place.Id, place.Name);
    }

    public async Task<IReadOnlyList<long>> GetRunIdsForPlaceAsync(int placeId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Set<RunActivityEntity>().AsNoTracking()
            .Where(run => run.PlaceId == placeId)
            .OrderByDescending(run => run.StartUtc)
            .Select(run => run.Id)
            .ToListAsync(ct);
    }

    public async Task RenameAsync(int placeId, string name, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(ct);

        var place = await db.Set<RunPlaceEntity>().FirstOrDefaultAsync(p => p.Id == placeId, ct);
        if (place is null)
        {
            return;
        }

        place.Name = trimmed.Length > 80 ? trimmed[..80] : trimmed;
        await db.SaveChangesAsync(ct);
    }
}
