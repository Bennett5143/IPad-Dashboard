namespace Dashboard.Domain.Running;

/// <summary>Schnellste Zeit eines Laufs über eine Zieldistanz; <see cref="FastestTime"/> fehlt, wenn zu kurz.</summary>
public sealed record BestEffort(double DistanceMeters, TimeSpan? FastestTime);

/// <summary>
/// „Best Efforts" je Lauf (FA-8.17-Umfeld): die schnellste zusammenhängende Teilstrecke über
/// 1/5/10 km, per Sliding-Window über die Pro-Punkt-Streams (kumulierte Distanz + Zeit).
/// Reine, testbare Logik – ohne Streams gibt es keine Bestzeiten.
/// </summary>
public static class BestEffortsCalculator
{
    public static readonly IReadOnlyList<double> StandardDistances = [1000, 5000, 10000];

    public static IReadOnlyList<BestEffort> Compute(Run run) => Compute(run, StandardDistances);

    public static IReadOnlyList<BestEffort> Compute(Run run, IReadOnlyList<double> targetsMeters)
    {
        var streams = run.Streams;
        var track = streams?.Track;
        if (streams is null || track is null || track.Count < 2 || streams.TimeOffsetsSeconds.Count != track.Count)
        {
            return targetsMeters.Select(t => new BestEffort(t, null)).ToList();
        }

        var n = track.Count;
        var cumulative = new double[n];
        for (var i = 1; i < n; i++)
        {
            cumulative[i] = cumulative[i - 1] + GeoMath.HaversineMeters(track[i - 1], track[i]);
        }

        var time = streams.TimeOffsetsSeconds;
        return targetsMeters.Select(target => new BestEffort(target, FastestWindow(cumulative, time, target))).ToList();
    }

    /// <summary>Kleinste Zeit eines Fensters, das mindestens <paramref name="target"/> Meter abdeckt.</summary>
    private static TimeSpan? FastestWindow(double[] cumulative, IReadOnlyList<int> time, double target)
    {
        if (target <= 0 || cumulative[^1] < target)
        {
            return null;
        }

        var best = int.MaxValue;
        var lo = 0;
        for (var hi = 0; hi < cumulative.Length; hi++)
        {
            // Fenster so weit verkleinern, wie es die Zieldistanz noch abdeckt – die engste
            // Variante je rechtem Rand liefert die kürzeste Zeit.
            while (cumulative[hi] - cumulative[lo] >= target)
            {
                best = Math.Min(best, time[hi] - time[lo]);
                lo++;
            }
        }

        return best == int.MaxValue ? null : TimeSpan.FromSeconds(best);
    }
}

/// <summary>Geo-Distanzen (Haversine) – zentral, damit Lauf-Auswertungen dasselbe Maß nutzen.</summary>
public static class GeoMath
{
    private const double EarthRadius = 6_371_000;
    private const double Deg2Rad = Math.PI / 180;

    public static double HaversineMeters(GeoPoint a, GeoPoint b)
    {
        var dLat = (b.Latitude - a.Latitude) * Deg2Rad;
        var dLng = (b.Longitude - a.Longitude) * Deg2Rad;
        var la1 = a.Latitude * Deg2Rad;
        var la2 = b.Latitude * Deg2Rad;
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(la1) * Math.Cos(la2) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * EarthRadius * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }
}
