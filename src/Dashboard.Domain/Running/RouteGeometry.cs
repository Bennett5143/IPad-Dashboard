namespace Dashboard.Domain.Running;

/// <summary>
/// Geometrie-Helfer für den Routenvergleich (FA-8.17) — reine, testbare Mathematik ohne
/// NetTopologySuite, damit die Domäne DB-frei bleibt. Punkte werden auf eine lokale
/// Meter-Ebene projiziert (äquirektangulär um einen Ursprung – für stadtweite Läufe genau
/// genug), vereinfacht (Douglas-Peucker) und über die diskrete Hausdorff-Distanz verglichen.
/// </summary>
public static class RouteGeometry
{
    private const double EarthRadius = 6_371_000;
    private const double Deg2Rad = Math.PI / 180;

    /// <summary>Projiziert einen Punkt relativ zum Ursprung in Meter (x = Ost, y = Nord).</summary>
    public static (double X, double Y) Project(GeoPoint point, GeoPoint origin)
    {
        var x = (point.Longitude - origin.Longitude) * Deg2Rad * EarthRadius * Math.Cos(origin.Latitude * Deg2Rad);
        var y = (point.Latitude - origin.Latitude) * Deg2Rad * EarthRadius;
        return (x, y);
    }

    public static IReadOnlyList<(double X, double Y)> ProjectAll(IReadOnlyList<GeoPoint> points, GeoPoint origin) =>
        points.Select(p => Project(p, origin)).ToList();

    /// <summary>Douglas-Peucker-Vereinfachung (iterativ); behält Start- und Endpunkt.</summary>
    public static IReadOnlyList<(double X, double Y)> Simplify(
        IReadOnlyList<(double X, double Y)> points, double toleranceMeters)
    {
        var n = points.Count;
        if (n < 3 || toleranceMeters <= 0)
        {
            return points.ToList();
        }

        var keep = new bool[n];
        keep[0] = keep[n - 1] = true;

        var stack = new Stack<(int Lo, int Hi)>();
        stack.Push((0, n - 1));
        while (stack.Count > 0)
        {
            var (lo, hi) = stack.Pop();
            double maxDist = 0;
            var index = -1;
            for (var i = lo + 1; i < hi; i++)
            {
                var d = PointToSegment(points[i], points[lo], points[hi]);
                if (d > maxDist)
                {
                    maxDist = d;
                    index = i;
                }
            }

            if (maxDist > toleranceMeters && index != -1)
            {
                keep[index] = true;
                stack.Push((lo, index));
                stack.Push((index, hi));
            }
        }

        var result = new List<(double X, double Y)>();
        for (var i = 0; i < n; i++)
        {
            if (keep[i])
            {
                result.Add(points[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// Symmetrische diskrete Hausdorff-Distanz (Meter): der größte „kleinste Abstand" eines
    /// Vertex zur jeweils anderen Polylinie. Richtungs-agnostisch (Punktmengen, keine Reihenfolge).
    /// </summary>
    public static double PolylineHausdorff(
        IReadOnlyList<(double X, double Y)> a, IReadOnlyList<(double X, double Y)> b)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return double.PositiveInfinity;
        }

        return Math.Max(DirectedHausdorff(a, b), DirectedHausdorff(b, a));
    }

    private static double DirectedHausdorff(
        IReadOnlyList<(double X, double Y)> from, IReadOnlyList<(double X, double Y)> to)
    {
        double worst = 0;
        foreach (var p in from)
        {
            double nearest = double.PositiveInfinity;
            for (var i = 0; i < to.Count - 1; i++)
            {
                nearest = Math.Min(nearest, PointToSegment(p, to[i], to[i + 1]));
            }
            if (to.Count == 1)
            {
                nearest = Distance(p, to[0]);
            }

            worst = Math.Max(worst, nearest);
        }

        return worst;
    }

    private static double PointToSegment((double X, double Y) p, (double X, double Y) a, (double X, double Y) b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var len2 = dx * dx + dy * dy;
        if (len2 <= 0)
        {
            return Distance(p, a);
        }

        var t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2, 0, 1);
        return Distance(p, (a.X + t * dx, a.Y + t * dy));
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
